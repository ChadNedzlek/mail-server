using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class HelloCommand : ICommandFactory
	{
		public string Name => "HELO";
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
				smtpSession.ConnectedHost = Arguments;
				return smtpSession.SendReplyAsync(ReplyCode.Okay, $"{smtpSession.DomainName} greets {Arguments}", token);
			}
		}
	}
}