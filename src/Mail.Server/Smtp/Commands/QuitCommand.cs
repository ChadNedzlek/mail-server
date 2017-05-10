using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("QUIT")]
	public class QuitCommand : BaseSmtpCommand
	{
		private readonly ISmtpMessageChannel _channel;

		public QuitCommand(ISmtpMessageChannel channel)
		{
			_channel = channel;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			await _channel.SendReplyAsync(SmtpReplyCode.Closing, "Ok", token);
			_channel.Close();
		}
	}
}
