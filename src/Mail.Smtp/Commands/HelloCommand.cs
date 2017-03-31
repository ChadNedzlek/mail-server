using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("HELO")]
    public class HelloCommand : BaseCommand
    {
        private readonly IMessageChannel _channel;
        private readonly SmtpSettings _settings;
        private readonly ILogger _log;

        public HelloCommand(
            IMessageChannel channel,
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