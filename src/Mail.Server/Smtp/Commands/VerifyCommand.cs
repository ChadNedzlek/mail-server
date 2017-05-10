using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("VRFY")]
	public class VerifyCommand : BaseSmtpCommand
	{
		private readonly ISmtpMessageChannel _channel;

		public VerifyCommand(ISmtpMessageChannel channel)
		{
			_channel = channel;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			return _channel.SendReplyAsync(SmtpReplyCode.CannotVerify, "Cannot verify", token);
		}
	}
}
