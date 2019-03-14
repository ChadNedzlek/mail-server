using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public class SpamAssassin : IIncomingMailScanner
	{
		private readonly AgentSettings _settings;
		private readonly IMailQueue _mailQueue;
		private readonly ILogger _logger;

		public SpamAssassin(
			AgentSettings settings,
			IMailQueue mailQueue,
			ILogger logger
		)
		{
			_settings = settings;
			_mailQueue = mailQueue;
			_logger = logger;
		}

		public async Task<Stream> ScanAsync(IMailReadReference mailReference, Stream stream)
		{
			var spamcPath = _settings.IncomingScan?.SpamAssassin?.ClientPath;
			if (String.IsNullOrEmpty(spamcPath))
			{
				return stream;
			}

			var position = stream.Position;

			ProcessStartInfo info = new ProcessStartInfo(spamcPath)
			{
				CreateNoWindow = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
			};

			var proc = new Process
			{
				StartInfo = info,
				EnableRaisingEvents = true,
			};

			TaskCompletionSource<int> exitSource = new TaskCompletionSource<int>();
			proc.Exited += (p, e) => exitSource.SetResult(((Process) p).ExitCode);
			proc.Start();

			Stream tempStream = _mailQueue.GetTemporaryMailStream(mailReference);

			await Task.WhenAll(
				stream.CopyToAsync(proc.StandardInput.BaseStream),
				exitSource.Task,
				proc.StandardOutput.BaseStream.CopyToAsync(tempStream)
			);

			if ((await exitSource.Task) != 0)
			{
				// spam assassin returns non-zero, abort and just return the original
				// we might not have anything to report, so just return the original stream
				stream.Position = position;
				return stream;
			}

			// Unfortunately, we need the score from the spam assassin to just the mail
			// And there is no way to both get the score AND adjust the message body at the same time
			// So we have to scan for a header we know about...
			double score = 0;

			string targetHeader = _settings.IncomingScan.SpamAssassin.ScoreHeader;

			using (StreamReader reader = new StreamReader(tempStream, Encoding.ASCII, false, 1024, true))
			{
				var line = await reader.ReadLineAsync();
				while (!String.IsNullOrEmpty(line))
				{
					if (line.StartsWith(targetHeader))
					{
						if (Double.TryParse(line.AsSpan(0, targetHeader.Length), out double dScore))
						{
							// We found a score-y line, that's the one!
							score = dScore;
							_logger.Verbose($"Mail {mailReference.Id} has spam score of {score}");
						}
						else
						{
							_logger.Warning($"Mail {mailReference.Id} has invalid spam assassin header: {line}");
						}

						// We found the header, whether it was the score or not, 
						break;
					}

					line = await reader.ReadLineAsync();
				}
			}

			if (score > _settings.IncomingScan.SpamAssassin.DeleteThreshold)
			{
				_logger.Information($"Mail {mailReference.Id} has spam score of {score}, which exceeds threshold of {_settings.IncomingScan.SpamAssassin.DeleteThreshold}, discarding.");
				// This was so high, we are just supposed to delete it without forwarding
				tempStream.Dispose();
				return null;
			}

			tempStream.Seek(0, SeekOrigin.Begin);
			return tempStream;
		}
	}
}
