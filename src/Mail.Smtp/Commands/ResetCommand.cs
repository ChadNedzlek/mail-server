using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("RSET")]
    public class ResetCommand : BaseCommand
    {
        private readonly IMessageChannel _channel;
        private readonly IMailBuilder _builder;

        public ResetCommand(IMessageChannel channel, IMailBuilder builder)
        {
            _channel = channel;
            _builder = builder;
        }

        public override Task ExecuteAsync(CancellationToken token)
        {
			_builder.PendingMail = null;
            return _channel.SendReplyAsync(ReplyCode.Okay, token);
        }
    }
}