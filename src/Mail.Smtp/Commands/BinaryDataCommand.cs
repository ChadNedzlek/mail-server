using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class BinaryDataCommand : ICommandFactory
	{
		public string Name => "BDAT";
		public ICommand CreateCommand(string arguments)
		{
			return new Implementation(Name, arguments);
		}

		private class Implementation : BaseCommand
		{
			public Implementation(string name, string arguments) : base(name, arguments)
			{
			}

			public override async Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token)
			{
                if (String.IsNullOrEmpty(smtpSession.PendingMail?.FromPath?.Mailbox) ||
					smtpSession.PendingMail?.Recipents?.Count == 0 ||
					smtpSession.PendingMail?.IsBinary != true)
				{
					await smtpSession.SendReplyAsync(ReplyCode.BadSequence, "Bad sequence", token);
					return;
				}

				string[] parts = Arguments?.Split(' ');
				if (parts == null || parts.Length == 0 || parts.Length > 2)
				{
					await smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Length required, optional LAST", token);
					return;
				}

				int length;
				if (!Int32.TryParse(parts[0], out length) || length < 1)
				{
					await smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Length must be positive integer", token);
					return;
				}

				bool last = false;
				if (parts.Length == 2)
				{
					if (!String.Equals("LAST", parts[1]))
					{
						await smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "LAST expected", token);
						return;
					}
					last = true;
				}

				using (var mailReference = await smtpSession.MailStore.NewMailAsync(smtpSession.PendingMail.FromPath.Mailbox, smtpSession.PendingMail.Recipents, token))
				{
					using (var mailStream = mailReference.BodyStream)
					{

						byte[] chunk = new byte[1000];
						int totalRead = 0;
						do
						{
							int toRead = Math.Min((int) chunk.Length, length - totalRead);
							int read = await smtpSession.Connection.ReadBytesAsync(chunk, 0, toRead, token);
							totalRead += read;
							await mailStream.WriteAsync(chunk, 0, read, token);
						} while (totalRead < length);
					}

					await mailReference.SaveAsync(token);
				}

				await smtpSession.SendReplyAsync(ReplyCode.Okay, $"Recieved {length} octets", token);

				if (last)
				{
					smtpSession.PendingMail = null;
					await smtpSession.SendReplyAsync(ReplyCode.Okay, "Message complete", token);
				}
			}
		}
	}
}