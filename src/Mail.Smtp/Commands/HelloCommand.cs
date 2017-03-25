using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("HELO")]
    public class HelloCommand : BaseCommand
    {
        private readonly IMessageChannel _channel;
        private readonly SmtpSettings _settings;

        public HelloCommand(
            IMessageChannel channel,
            SmtpSettings settings)
        {
            _channel = channel;
            _settings = settings;
        }

        public override Task ExecuteAsync(CancellationToken token)
        {
			_channel.ConnectedHost = Arguments;
            return _channel.SendReplyAsync(
                ReplyCode.Okay,
                $"{_settings.DomainName} greets {Arguments}",
                token);
        }
    }
}