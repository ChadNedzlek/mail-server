using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using Newtonsoft.Json;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Transfer
{
	public class MailTransfer
	{
		private static readonly ImmutableArray<TimeSpan> s_retryDelays = ImmutableArray.Create(
			TimeSpan.Zero,
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(30),
			TimeSpan.FromHours(1),
			TimeSpan.FromHours(2),
			TimeSpan.FromHours(3),
			TimeSpan.FromHours(4),
			TimeSpan.FromHours(6),
			TimeSpan.FromHours(12),
			TimeSpan.FromHours(24),
			TimeSpan.FromHours(48),
			TimeSpan.FromHours(72)
		);

		private readonly ILogger _log;
		private readonly IMailTransferQueue _queue;
		private readonly IVolatile<SmtpSettings> _settings;
		private Lazy<Dictionary<string, SmtpFailureData>> _failures;

		public MailTransfer(IMailTransferQueue queue, IVolatile<SmtpSettings> settings, ILogger log)
		{
			_queue = queue;
			_settings = settings;
			_log = log;
			_failures = new Lazy<Dictionary<string,SmtpFailureData>>(LoadSavedFailureData);
		}

		private Dictionary<string, SmtpFailureData> LoadSavedFailureData()
		{
			string serializedPath = Path.Combine(_settings.Value.WorkingDirectory, "relay-failures.json");
			if (!File.Exists(serializedPath))
				return new Dictionary<string, SmtpFailureData>();

			try
			{
				using (var stream = File.Open(serializedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var reader = new StreamReader(stream))
				using (var jsonReader = new JsonTextReader(reader))
				{
					return new JsonSerializer().Deserialize<Dictionary<string,SmtpFailureData>>(jsonReader);
				}
			}
			catch (IOException e)
			{
				LogExtentions.Warning(_log, $"Failed to load {serializedPath}, using no existing failures: {e}");
				return new Dictionary<string, SmtpFailureData>();
			}
		}

		private void SaveFailureData()
		{
			string serializedPath = Path.Combine(_settings.Value.WorkingDirectory, "relay-failures.json");

			if (!_failures.IsValueCreated || _failures.Value.Count == 0)
			{
				try
				{
					File.Delete(serializedPath);
				}
				catch (Exception)
				{
					LogExtentions.Warning(_log, $"Failed to delete {serializedPath}");
				}

				return;
			}

			using (var stream = File.Open(serializedPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var reader = new StreamWriter(stream))
			using (var jsonReader = new JsonTextWriter(reader))
			{
				new JsonSerializer().Serialize(jsonReader, _failures.Value);
			}
		}

		public async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					bool sent = false;
					foreach (string domain in _queue.GetMailsByDomain())
					{
						List<IMailReference> mails = Enumerable.ToList<IMailReference>(_queue.GetAllMailForDomain(domain).Where(IsReadyToSend));
						if (mails.Count == 0)
						{
							continue;
						}
						sent = true;
						await SendMailsToDomain(domain, mails, token);
					}

					SaveFailureData();

					if (!sent)
					{
						LogExtentions.Verbose(_log, "No mails to send, sleeping");
						await Task.Delay(_settings.Value.IdleDelay ?? 30000, token);
					}
				}
				catch (Exception e)
				{
					LogExtentions.Error(_log, "Failed processing outgoing mail queues", e);
				}
			}
		}

		private async Task SendMailsToDomain(string domain, List<IMailReference> mails, CancellationToken token)
		{
			LogExtentions.Verbose(_log, $"Sending outbound mails for {domain}");
			var dns = new LookupClient();
			IDnsQueryResponse dnsResponse = await dns.QueryAsync(domain, QueryType.MX, token);
			foreach (MxRecord mxRecord in dnsResponse.Answers.MxRecords().OrderBy(mx => mx.Preference))
			{
				LogExtentions.Verbose(_log, $"Resolved MX record {mxRecord.Exchange} at priority {mxRecord.Preference}");
				if (await TrySendToMx(domain, mxRecord.Exchange, dns, mails, token))
				{
					return;
				}
			}

			LogExtentions.Warning(_log, $"Unable to send to MX for {domain}");
		}

		private async Task<bool> TrySendToMx(
			string domain,
			DnsString target,
			LookupClient dns,
			List<IMailReference> mails,
			CancellationToken token)
		{
			LogExtentions.Verbose(_log, $"Looking up information for MX {target}");
			IDnsQueryResponse aRecord = await dns.QueryAsync(target, QueryType.A, token);
			IPAddress targetIp = aRecord.Answers.ARecords().FirstOrDefault()?.Address;
			if (targetIp == null)
			{
				IDnsQueryResponse aaaaRecord = await dns.QueryAsync(target, QueryType.AAAA, token);
				targetIp = aaaaRecord.Answers.AaaaRecords().FirstOrDefault()?.Address;
			}

			if (targetIp == null)
			{
				LogExtentions.Warning(_log, $"Failed to resolve A or AAAA record for MX record {target}");
				return false;
			}

			SmtpRelayDomain relayDescription = _settings.Value.RelayDomains?
				.FirstOrDefault(r => string.Equals(r.Name, domain, StringComparison.OrdinalIgnoreCase));

			int port = relayDescription?.Port ?? 25;
			using (var mxClient = new TcpClient())
			{
				LogExtentions.Information(_log, $"Connecting to MX {target} at {targetIp} on port {port}");
				await mxClient.ConnectAsync(targetIp, port);

				using (NetworkStream mxStream = mxClient.GetStream())
				using (var stream = new RedirectableStream(mxStream))
				using (var reader = new StreamReader(stream))
				using (var writer = new StreamWriter(stream))
				{
					await SendCommand(writer, $"EHLO {_settings.Value.DomainName}");
					SmtpResponse response = await ReadResponse(reader);

					var startTls = false;
					if (response.Code == ReplyCode.Okay)
					{
						startTls = response.Lines.Contains("STARTTLS", StringComparer.OrdinalIgnoreCase);
					}
					else
					{
						await SendCommand(writer, $"HELO {_settings.Value.DomainName}");
						response = await ReadResponse(reader);
					}

					if (response.Code != ReplyCode.Okay)
					{
						LogExtentions.Warning(_log, "Failed to HELO/EHLO, aborting");
						return false;
					}

					if (startTls)
					{
						await SendCommand(writer, "STARTTLS");
						response = await ReadResponse(reader);
						if (response.Code != ReplyCode.Okay)
						{
							LogExtentions.Warning(_log, "Failed to STARTTLS, aborting");
							return false;
						}

						var sslStream = new SslStream(mxStream, true);
						await sslStream.AuthenticateAsClientAsync(target.Value);
						stream.ChangeSteam(sslStream);
					}

					var allSuccess = true;
					foreach (IMailReference mail in mails)
					{
						if (!IsReadyToSend(mail))
						{
							continue;
						}

						if (await SendSingleMailAsync(token, mail, writer, reader))
						{
							_failures.Value.Remove(mail.Id);
							continue;
						}

						if (!ShouldAttemptRedelivery(mail))
						{
							await HandleRejectedMailAsync(mail);
							_failures.Value.Remove(mail.Id);
							await _queue.DeleteAsync(mail);
						}
						allSuccess = false;
					}

					if (!allSuccess)
					{
						LogExtentions.Warning(_log, "Failed to send at least one mail");
						return false;
					}

					await SendCommand(writer, "QUIT");
				}
			}

			return true;
		}

		private Task HandleRejectedMailAsync(IMailReference mail)
		{
			LogExtentions.Warning(_log, $"Max reties attempted for mail {mail.Id}, deleting");
			return Task.CompletedTask;
		}

		private bool ShouldAttemptRedelivery(IMailReference mail)
		{
			if (!_failures.Value.TryGetValue(mail.Id, out var failure))
			{
				failure = new SmtpFailureData(mail.Id) {LastAttempt = DateTimeOffset.UtcNow, Retries = 0};
				_failures.Value.Add(mail.Id, failure);
				return true;
			}

			if (failure.Retries + 1 >= s_retryDelays.Length)
			{
				return false;
			}

			failure.Retries++;
			failure.LastAttempt = DateTimeOffset.UtcNow;
			return true;
		}

		private bool IsReadyToSend(IMailReference mail)
		{
			if (!_failures.Value.TryGetValue(mail.Id, out var failureData))
			{
				return true;
			}

			TimeSpan currentLag = DateTimeOffset.UtcNow - failureData.LastAttempt;
			if (currentLag < CalculateNextRetryInterval(failureData.Retries))
			{
				return false;
			}

			return true;
		}

		private static TimeSpan? CalculateNextRetryInterval(int retries)
		{
			if (retries >= s_retryDelays.Length)
			{
				return null;
			}
			return s_retryDelays[retries];
		}

		private async Task<bool> SendSingleMailAsync(
			CancellationToken token,
			IMailReference mail,
			StreamWriter writer,
			StreamReader reader)
		{
			LogExtentions.Information(_log, $"Sending mail {mail.Id}");
			IMailReadReference readMail = await _queue.OpenReadAsync(mail, token);
			LogExtentions.Information(_log, $"Sender: {readMail.Sender}, Recipients: {string.Join(",", readMail.Recipients)}");
			await SendCommand(writer, $"MAIL FROM:<{readMail.Sender}>");
			SmtpResponse response = await ReadResponse(reader);
			if (response.Code != ReplyCode.Okay)
			{
				LogExtentions.Warning(_log, "Failed MAIL FROM, aborting");
				return false;
			}

			foreach (string recipient in readMail.Recipients)
			{
				await SendCommand(writer, $"RCPT TO:<{recipient}>");
				response = await ReadResponse(reader);
				if (response.Code != ReplyCode.Okay)
				{
					LogExtentions.Warning(_log, "Failed RCPT TO, aborting");
					return false;
				}
			}

			await SendCommand(writer, "DATA");
			response = await ReadResponse(reader);
			if (response.Code != ReplyCode.Okay)
			{
				LogExtentions.Warning(_log, "Failed RCPT TO, aborting");
				return false;
			}

			using (var mailReader = new StreamReader(readMail.BodyStream))
			{
				string line = null;
				while (await mailReader.TryReadLineAsync(l => line = l, token))
				{
					await writer.WriteLineAsync(line);
				}
			}

			await writer.WriteLineAsync(".");
			if (response.Code != ReplyCode.Okay)
			{
				LogExtentions.Warning(_log, "Failed RCPT TO, aborting");
				return false;
			}

			await _queue.DeleteAsync(mail);

			return true;
		}

		private async Task<SmtpResponse> ReadResponse(StreamReader reader)
		{
			var lines = new List<string>();
			var more = true;
			ReplyCode? currentReply = null;
			while (more)
			{
				string line = await reader.ReadLineAsync();
				LogExtentions.Verbose(_log, line);

				if (line.Length < 3)
				{
					LogExtentions.Warning(_log, $"Illegal response: {line}");
					return null;
				}
				if (!int.TryParse(line.Substring(0, 3), out var reponseCode))
				{
					LogExtentions.Warning(_log, $"Illegal response: {line}");
					return null;
				}
				var newReply = (ReplyCode) reponseCode;
				if (currentReply.HasValue && newReply != currentReply.Value)
				{
					LogExtentions.Warning(_log, $"Illegal contiuation: Previous reply {currentReply}, new line: {line}");
					return null;
				}
				currentReply = newReply;
				more = line.Length >= 4 && line[3] == '-';
				lines.Add(line.Substring(Math.Max(line.Length, 4)));
			}

			return new SmtpResponse(currentReply.Value, lines);
		}

		private Task SendCommand(StreamWriter writer, string command)
		{
			LogExtentions.Verbose(_log, command);
			return writer.WriteLineAsync(command);
		}
	}
}