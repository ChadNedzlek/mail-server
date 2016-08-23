using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class DataCommand : ICommandFactory
	{
		public string Name => "DATA";
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
					smtpSession.PendingMail?.IsBinary == true)
				{
					await smtpSession.SendReplyAsync(ReplyCode.BadSequence, "Bad sequence", token);
					return;
				}

				await smtpSession.SendReplyAsync(ReplyCode.StartMail, "Send data, end with .<CR><LF>", token);
				using (var mailStream = await smtpSession.MailStore.NewMailAsync(smtpSession.PendingMail.FromPath.Mailbox,smtpSession.PendingMail.Recipents,token))
				using (var mailWriter = new StreamWriter(mailStream.BodyStream, Encoding.UTF8))
				{
					await
						mailWriter.WriteLineAsync(
							$"Received: FROM {smtpSession.ConnectedHost} ({smtpSession.ConnectedIpAddress}) BY {smtpSession.Settings.DomainName} ({smtpSession.IpAddress}); {DateTime.UtcNow:ddd, dd MMM yyy HH:mm:ss zzzz}");

					string line;
					while ((line = await smtpSession.Connection.ReadLineAsync(Encoding.UTF8, token)) != ".")
					{
						await mailWriter.WriteLineAsync(line);
					}
				}


				smtpSession.PendingMail = null;
				await smtpSession.SendReplyAsync(ReplyCode.Okay, "OK", token);
			}
		}
	}
}