using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("NOOP")]
	public class NoopCommand : BaseSmtpCommand
	{
		private readonly IMessageChannel _channel;

		public NoopCommand(IMessageChannel channel)
		{
			_channel = channel;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			return _channel.SendReplyAsync(ReplyCode.Okay, "Noop", token);
		}
	}
}
