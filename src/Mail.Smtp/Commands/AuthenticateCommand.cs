using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class AuthenticateCommand : ICommandFactory
	{
		public string Name => "AUTH";
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
				string[] parts = Arguments.Split(new [] { ' '}, 2);
				if (parts == null || parts.Length == 0 || parts.Length > 2)
				{
					await smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Expected mechanism and optional initial response", token);
					return;
				}
				string mechanismName = parts[0];
				IAuthenticationMechanism mechanism = smtpSession.ImplementationFactory.Authentication.Get(mechanismName);
				if (mechanism == null)
				{
					await smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Unknown mechanism", token);
					return;
				}

				var authSession = mechanism.CreateSession(smtpSession);
				UserData userData;

				try
				{
					userData = await authSession.AuthenticateAsync(smtpSession.UserStore, token, false);
				}
				catch (ArgumentException)
				{
					await smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Invalid arguments", token);
					return;
				}

				smtpSession.AuthenticatedUser = userData;
				await smtpSession.SendReplyAsync(ReplyCode.AuthenticationComplete, "Authentication unsuccessful", token);
			}
		}
	}
}