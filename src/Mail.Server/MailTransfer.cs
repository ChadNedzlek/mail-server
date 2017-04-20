using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
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
		private readonly IMailSendFailureManager _failures;
		private readonly ITcpConnectionProvider _tcp;
		private readonly IDnsResolve _dns;

		public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

		public MailTransfer(IMailTransferQueue queue, IVolatile<SmtpSettings> settings, ILogger log, IDnsResolve dns, IMailSendFailureManager failures, ITcpConnectionProvider tcp)
		{
			_queue = queue;
			_settings = settings;
			_log = log;
			_dns = dns;
			_failures = failures;
			_tcp = tcp;
		}

		public async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					bool sent = false;
					foreach (string domain in _queue.GetAllPendingDomains())
					{
						List<IMailReference> mails = _queue.GetAllMailForDomain(domain).Where(IsReadyToSend).ToList();
						if (mails.Count == 0)
						{
							continue;
						}
						sent = true;
						await SendMailsToDomain(domain, mails, token);
					}

					_failures.SaveFailureData();

					if (!sent)
					{
						_log.Verbose("No mails to send, sleeping");
						await Task.Delay(_settings.Value.IdleDelay ?? 30000, token);
					}
				}
				catch (Exception e)
				{
					_log.Error("Failed processing outgoing mail queues", e);
				}
			}
		}

		public async Task SendMailsToDomain(string domain, IReadOnlyList<IMailReference> mails, CancellationToken token)
		{
			_log.Verbose($"Sending outbound mails for {domain}");

			SmtpRelayDomain relayDomain = _settings.Value.RelayDomains?.FirstOrDefault(rd => string.Equals(rd.Name, domain, StringComparison.OrdinalIgnoreCase));
			if (relayDomain != null)
			{
				_log.Verbose($"Relayed mail detected, replaying from {relayDomain.Name} to {relayDomain.Relay}:{relayDomain.Port}");
				IReadOnlyList<IMailReference> unsent = await TrySendToMx(relayDomain.Relay, mails, relayDomain.Port ?? 25, token);
				if ((unsent?.Count ?? 0) != 0)
				{
					await HandleFailedMailsAsync(unsent);
					_log.Warning("Relay failed");
				}
				return;
			}

			foreach (DnsMxRecord mxRecord in (await _dns.QueryMx(domain, token)).OrderBy(mx => mx.Preference))
			{
				if (IsSendToSelf(mxRecord.Exchange))
				{
					_log.Warning("Detected send to self in preference fallback, aborting");
					break;
				}
				_log.Verbose($"Resolved MX record {mxRecord.Exchange} at priority {mxRecord.Preference}");
				mails = await TrySendToMx(mxRecord.Exchange, mails, 25, token);

				if ((mails?.Count ?? 0) == 0)
				{
					return;
				}

				_log.Warning($"Failed to send {mails.Count} emails to {mxRecord.Exchange}, trying next");
			}

			await HandleFailedMailsAsync(mails);

			_log.Warning($"Unable to send to MX for {domain}");
		}

		private async Task HandleFailedMailsAsync(IReadOnlyList<IMailReference> unsent)
		{
			foreach (var mail in unsent)
			{
				SmtpFailureData retryData = ShouldAttemptRedelivery(mail);
				if (retryData == null)
				{
					// We're giving up, forget about it
					_failures.RemoveFailure(mail.Id);
					await HandleRejectedMailAsync(mail);
					await _queue.DeleteAsync(mail);
				}
				else
				{
					retryData.Retries++;
				}
			}
		}

		private bool IsSendToSelf(string exchange)
		{
			if (String.Equals(_settings.Value.DomainName, exchange, StringComparison.OrdinalIgnoreCase))
				return true;

			if (_settings.Value.DomainAliases?.Contains(exchange, StringComparer.OrdinalIgnoreCase) == true)
				return true;

			return false;
		}

		private async Task<IReadOnlyList<IMailReference>> TrySendToMx(
			string target,
			IReadOnlyList<IMailReference> mails,
			int port,
			CancellationToken token)
		{
			_log.Verbose($"Looking up information for MX {target}");
			IPAddress targetIp = await _dns.QueryIp(target,  token);

			if (targetIp == null)
			{
				_log.Warning($"Failed to resolve A or AAAA record for MX record {target}");
				return mails;
			}
			using (var mxClient = _tcp.GetClient())
			{
				_log.Information($"Connecting to MX {target} at {targetIp} on port {port}");
				await mxClient.ConnectAsync(targetIp, port);

				using (Stream mxStream = mxClient.GetStream())
				{
					mails = await TrySendMailsToStream(target, mails, mxStream, token);
				}
			}

			return mails;
		}

		public async Task<IReadOnlyList<IMailReference>> TrySendMailsToStream(string target, IReadOnlyList<IMailReference> mails, Stream mxStream, CancellationToken token)
		{
			List<IMailReference> unsent = new List<IMailReference>();
			using (var stream = new RedirectableStream(mxStream))
			using (var reader = new StreamReader(stream))
			using (var writer = new StreamWriter(stream))
			{
				SmtpResponse response = await ExecuteRemoteCommandAsync(writer, reader, $"EHLO {_settings.Value.DomainName}");

				var startTls = false;
				if (response.Code == ReplyCode.Greeting)
				{
					startTls = response.Lines.Contains("STARTTLS", StringComparer.OrdinalIgnoreCase);
				}
				else
				{
					response = await ExecuteRemoteCommandAsync(writer, reader, $"HELO {_settings.Value.DomainName}");
				}

				if (response.Code != ReplyCode.Greeting)
				{
					_log.Warning("Failed to HELO/EHLO, aborting");
					return mails;
				}

				if (startTls)
				{
					response = await ExecuteRemoteCommandAsync(writer, reader, "STARTTLS");
					if (response.Code != ReplyCode.Okay)
					{
						_log.Warning("Failed to STARTTLS, aborting");
						return mails;
					}

					var sslStream = new SslStream(mxStream, true, RemoteCertificateValidationCallback);
					try
					{
						await sslStream.AuthenticateAsClientAsync(target);
					}
					catch (Exception e)
					{
						_log.Warning($"Failed TLS negotiations: {e}");
						return mails;
					}
					stream.ChangeSteam(sslStream);
				}

				var allSuccess = true;
				foreach (IMailReference mail in mails)
				{
					if (!IsReadyToSend(mail))
					{
						unsent.Add(mail);
						continue;
					}

					if (await TrySendSingleMailAsync(mail, writer, reader, token))
					{
						_failures.RemoveFailure(mail.Id);
						continue;
					}

					unsent.Add(mail);
					allSuccess = false;
				}

				if (!allSuccess)
				{
					_log.Warning("Failed to send at least one mail");
					return unsent;
				}

				await SendCommandAsync(writer, "QUIT");
			}
			return unsent;
		}

		private Task HandleRejectedMailAsync(IMailReference mail)
		{
			_log.Warning($"Max reties attempted for mail {mail.Id}, deleting");
			return Task.CompletedTask;
		}

		public SmtpFailureData ShouldAttemptRedelivery(IMailReference mail)
		{
			SmtpFailureData failure = _failures.GetFailure(mail.Id, true);
			if (failure.Retries + 1 >= s_retryDelays.Length)
			{
				return null;
			}

			return failure;
		}

		public bool IsReadyToSend(IMailReference mail)
		{
			SmtpFailureData failure = _failures.GetFailure(mail.Id, false);
			if (failure == null)
				return true;

			TimeSpan currentLag = DateTimeOffset.UtcNow - failure.FirstFailure;
			if (currentLag < CalculateNextRetryInterval(failure.Retries))
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

		public async Task<bool> TrySendSingleMailAsync(
			IMailReference mail,
			StreamWriter writer,
			StreamReader reader,
			CancellationToken token)
		{
			_log.Information($"Sending mail {mail.Id}");
			IMailReadReference readMail = await _queue.OpenReadAsync(mail, token);
			_log.Information($"Sender: {readMail.Sender}, Recipients: {string.Join(",", readMail.Recipients)}");
			SmtpResponse response = await ExecuteRemoteCommandAsync(writer, reader, $"MAIL FROM:<{readMail.Sender}>");
			if (response.Code != ReplyCode.Okay)
			{
				_log.Warning("Failed MAIL FROM, aborting");
				return false;
			}

			foreach (string recipient in readMail.Recipients)
			{
				response = await ExecuteRemoteCommandAsync(writer, reader, $"RCPT TO:<{recipient}>");
				if (response.Code != ReplyCode.Okay)
				{
					_log.Warning("Failed RCPT TO, aborting");
					return false;
				}
			}

			response = await ExecuteRemoteCommandAsync(writer, reader, "DATA");
			if (response.Code != ReplyCode.StartMail)
			{
				_log.Warning("Failed DATA, aborting");
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
			response = await ReadResponseAsync(reader);
			if (response.Code != ReplyCode.Okay)
			{
				_log.Warning("Failed RCPT TO, aborting");
				return false;
			}

			await _queue.DeleteAsync(mail);

			return true;
		}

		private async Task<SmtpResponse> ReadResponseAsync(TextReader reader)
		{
			var lines = new List<string>();
			var more = true;
			ReplyCode? currentReply = null;
			while (more)
			{
				string line = await reader.ReadLineAsync();
				_log.Verbose(line);

				if (line.Length < 3)
				{
					_log.Warning($"Illegal response: {line}");
					return null;
				}
				if (!int.TryParse(line.Substring(0, 3), out var reponseCode))
				{
					_log.Warning($"Illegal response: {line}");
					return null;
				}
				var newReply = (ReplyCode) reponseCode;
				if (currentReply.HasValue && newReply != currentReply.Value)
				{
					_log.Warning($"Illegal contiuation: Previous reply {currentReply}, new line: {line}");
					return null;
				}
				currentReply = newReply;
				more = line.Length >= 4 && line[3] == '-';
				lines.Add(line.Substring(Math.Min(line.Length, 4)));
			}

			return new SmtpResponse(currentReply.Value, lines);
		}

		private async Task SendCommandAsync(TextWriter writer, string command)
		{
			_log.Verbose(command);
			await writer.WriteLineAsync(command);
			await writer.FlushAsync();
		}

		public async Task<SmtpResponse> ExecuteRemoteCommandAsync(StreamWriter writer, StreamReader reader, string command)
		{
			await SendCommandAsync(writer, command);
			return await ReadResponseAsync(reader);
		}
	}
}