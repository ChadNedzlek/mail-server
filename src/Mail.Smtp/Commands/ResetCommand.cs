using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class ResetCommand : ICommandFactory
	{
		public string Name => "RSET";
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
				smtpSession.PendingMail = null;
				return smtpSession.SendReplyAsync(ReplyCode.Okay, token);
			}
		}
	}
}