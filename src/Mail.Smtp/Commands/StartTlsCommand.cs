using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class StartTlsCommand : ICommandFactory
	{
		public string Name => "STARTTLS";
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
				await smtpSession.SendReplyAsync(ReplyCode.Greeting, "Ready to start TLS", token);
				await smtpSession.Connection.NegotiateTlsAsync();
			}
		}
	}
}