using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[Command("BDAT")]
	public class BinaryDataCommand : BaseCommand
	{
		private readonly IMailBuilder _builder;
		private readonly SecurableConnection _connection;
		private readonly IMailQueue _mailQueue;
		private readonly SmtpSession _session;

		public BinaryDataCommand(
			SecurableConnection connection,
			SmtpSession session,
			IMailQueue mailQueue,
			IMailBuilder builder)
		{
			_connection = connection;
			_session = session;
			_mailQueue = mailQueue;
			_builder = builder;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			if (string.IsNullOrEmpty(_builder.PendingMail?.FromPath?.Mailbox) ||
				_builder.PendingMail?.Recipents?.Count == 0 ||
				_builder.PendingMail?.IsBinary != true)
			{
				await _session.SendReplyAsync(ReplyCode.BadSequence, "Bad sequence", token);
				return;
			}

			string[] parts = Arguments?.Split(' ');
			if (parts == null || parts.Length == 0 || parts.Length > 2)
			{
				await _session.SendReplyAsync(ReplyCode.InvalidArguments, "Length required, optional LAST", token);
				return;
			}

			int length;
			if (!int.TryParse(parts[0], out length) || length < 1)
			{
				await _session.SendReplyAsync(ReplyCode.InvalidArguments, "Length must be positive integer", token);
				return;
			}

			var last = false;
			if (parts.Length == 2)
			{
				if (!string.Equals("LAST", parts[1]))
				{
					await _session.SendReplyAsync(ReplyCode.InvalidArguments, "LAST expected", token);
					return;
				}
				last = true;
			}

			using (IMailWriteReference mailReference = await _mailQueue.NewMailAsync(
				_builder.PendingMail.FromPath.Mailbox,
				_builder.PendingMail.Recipents.ToImmutableList(),
				token))
			{
				using (Stream mailStream = mailReference.BodyStream)
				{
					var chunk = new byte[1000];
					var totalRead = 0;
					do
					{
						int toRead = Math.Min(chunk.Length, length - totalRead);
						int read = await _connection.ReadBytesAsync(chunk, 0, toRead, token);
						totalRead += read;
						await mailStream.WriteAsync(chunk, 0, read, token);
					} while (totalRead < length);
				}

				await _mailQueue.SaveAsync(mailReference, token);
			}

			await _session.SendReplyAsync(ReplyCode.Okay, $"Recieved {length} octets", token);

			if (last)
			{
				_builder.PendingMail = null;
				await _session.SendReplyAsync(ReplyCode.Okay, "Message complete", token);
			}
		}
	}
}
