using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class ExtendedHelloCommand : ICommandFactory
	{
		public string Name => "EHLO";

		public ICommand CreateCommand(string arguments)
		{
			return new Implementation(Name, arguments);
		}

		private class Implementation : BaseCommand
		{
			public Implementation(string name, string arguments) : base(name, arguments)
			{
			}

			private static readonly ImmutableList<string> GeneralExtensions = ImmutableList.CreateRange(
				new[]
				{
					"8BITMIME",
					"UTF8SMTP",
                    "SMTPUTF8",
					"CHUNKING",
					"BINARYMIME",
				});

			private static readonly ImmutableList<string> PlainTextExtensions = ImmutableList.CreateRange(
				new[]
				{
					"STARTTLS"
				});


			public override async Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token)
			{
				ImmutableList<string> encryptedExtensions = ImmutableList.CreateRange(
					new[]
					{
						"AUTH " + String.Join(" ", smtpSession.ImplementationFactory.Authentication.GetSupported()),
					});

				smtpSession.ConnectedHost = Arguments;
				ImmutableList<string> plainTextExtensions = PlainTextExtensions;
				var extensions =
					GeneralExtensions.Concat(smtpSession.Connection.IsEncrypted ? encryptedExtensions : plainTextExtensions);

				if (smtpSession.Connection.Certificate != null && !smtpSession.Connection.IsEncrypted)
				{
					extensions = extensions.Concat(new[] {"STARTTLS"});
				}

				await smtpSession.SendReplyAsync(ReplyCode.Okay, true, $"{smtpSession.Settings.DomainName} greets {Arguments}", token);
				await smtpSession.SendReplyAsync(ReplyCode.Okay, extensions, token);
			}
		}
	}
}
