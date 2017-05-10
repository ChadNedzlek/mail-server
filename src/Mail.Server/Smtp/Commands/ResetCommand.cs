using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("RSET")]
	public class ResetCommand : BaseSmtpCommand
	{
		private readonly IMailBuilder _builder;
		private readonly ISmtpMessageChannel _channel;

		public ResetCommand(ISmtpMessageChannel channel, IMailBuilder builder)
		{
			_channel = channel;
			_builder = builder;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			_builder.PendingMail = null;
			return _channel.SendReplyAsync(SmtpReplyCode.Okay, token);
		}
	}
}
