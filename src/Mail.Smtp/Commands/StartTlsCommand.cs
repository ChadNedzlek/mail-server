using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("STARTTLS")]
    public class StartTlsCommand : BaseCommand
    {
        private readonly IMessageChannel _channel;
        private readonly SecurableConnection _connection;

        public StartTlsCommand(IMessageChannel channel, SecurableConnection connection)
        {
            _channel = channel;
            _connection = connection;
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            await _channel.SendReplyAsync(ReplyCode.Greeting, "Ready to start TLS", token);
            await _connection.NegotiateTlsAsync();
        }
    }
}
