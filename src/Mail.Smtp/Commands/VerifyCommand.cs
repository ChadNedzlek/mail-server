using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class VerifyCommand : ICommandFactory
	{
		public string Name => "VRFY";
		public ICommand CreateCommand(string arguments)
		{
			return new Implementation(Name, arguments);
		}

		private class Implementation : BaseCommand
		{
			public Implementation(string name, string arguments) : base(name, arguments)
			{
			}

			public override Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token)
			{
				return smtpSession.SendReplyAsync(ReplyCode.CannotVerify, "Cannot verify", token);
			}
		}
	}
}