using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("VRFY")]
    public class VerifyCommand : BaseCommand
    {
        private readonly IMessageChannel _channel;

        public VerifyCommand(IMessageChannel channel)
        {
            _channel = channel;
        }

        public override Task ExecuteAsync(CancellationToken token)
        {
            return _channel.SendReplyAsync(ReplyCode.CannotVerify, "Cannot verify", token);
        }
    }
}