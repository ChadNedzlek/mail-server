using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("DATA")]
	public class DataCommand : BaseSmtpCommand
	{
		private readonly IMailBuilder _builder;
		private readonly ISmtpMessageChannel _channel;
		private readonly SecurableConnection _connection;
		private readonly ConnectionInformation _connectionInformation;
		private readonly IMailQueue _mailQueue;
		private readonly AgentSettings _settings;

		public DataCommand(
			IMailQueue mailQueue,
			AgentSettings settings,
			SecurableConnection connection,
			ConnectionInformation connectionInformation,
			IMailBuilder builder,
			ISmtpMessageChannel channel
		)
		{
			_mailQueue = mailQueue;
			_settings = settings;
			_connection = connection;
			_connectionInformation = connectionInformation;
			_builder = builder;
			_channel = channel;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			if (string.IsNullOrEmpty(_builder.PendingMail?.FromPath?.Mailbox) ||
				_builder.PendingMail?.Recipents?.Count == 0 ||
				_builder.PendingMail?.IsBinary == true)
			{
				await _channel.SendReplyAsync(SmtpReplyCode.BadSequence, "Bad sequence", token);
				return;
			}

			await _channel.SendReplyAsync(SmtpReplyCode.StartMail, "Send data, end with .<CR><LF>", token);

			bool rejected = false;

			using (IMailWriteReference reference = await _mailQueue.NewMailAsync(
				_builder.PendingMail.FromPath.Mailbox,
				_builder.PendingMail.Recipents.ToImmutableList(),
				token))
			{
				using (var mailWriter = new StreamWriter(reference.BodyStream, Encoding.UTF8))
				{
					await mailWriter.WriteLineAsync(
						$"Received: FROM {_channel.ConnectedHost} ({_connectionInformation.RemoteAddress}) BY {_settings.DomainName} ({_connectionInformation.LocalAddress}); {DateTime.UtcNow:ddd, dd MMM yyy HH:mm:ss zzzz}");

					string line;
					while ((line = await _connection.ReadLineAsync(Encoding.UTF8, token)) != ".")
					{
						if (!_channel.IsAuthenticated &&
							_settings.UnauthenticatedMessageSizeLimit != 0 &&
							_settings.UnauthenticatedMessageSizeLimit <= mailWriter.BaseStream.Length)
						{
							if (!rejected)
							{
								await _channel.SendReplyAsync(SmtpReplyCode.ExceededQuota, "Message rejected, too large", token);
								mailWriter.Dispose();
								reference.Dispose();
								rejected = true;
							}

							continue;
						}

						await mailWriter.WriteLineAsync(line);
					}
				}

				if (!rejected)
				{
					await _mailQueue.SaveAsync(reference, token);
				}
			}

			_builder.PendingMail = null;
			if (!rejected)
			{
				await _channel.SendReplyAsync(SmtpReplyCode.Okay, "OK", token);
			}
		}
	}
}
