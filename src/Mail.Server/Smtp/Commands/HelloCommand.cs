using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("HELO")]
	public class HelloCommand : BaseSmtpCommand
	{
		private readonly ISmtpMessageChannel _channel;
		private readonly ILogger _log;
		private readonly SmtpSettings _settings;

		public HelloCommand(
			ISmtpMessageChannel channel,
			SmtpSettings settings,
			ILogger log)
		{
			_channel = channel;
			_settings = settings;
			_log = log;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			_channel.ConnectedHost = Arguments;

			_log.Information($"HELO from {Arguments}");
			return _channel.SendReplyAsync(
				ReplyCode.Okay,
				$"{_settings.DomainName} greets {Arguments}",
				token);
		}
	}
}
