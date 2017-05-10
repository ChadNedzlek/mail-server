using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("NOOP")]
	public class NoopCommand : BaseSmtpCommand
	{
		private readonly ISmtpMessageChannel _channel;

		public NoopCommand(ISmtpMessageChannel channel)
		{
			_channel = channel;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			return _channel.SendReplyAsync(SmtpReplyCode.Okay, "Noop", token);
		}
	}
}
