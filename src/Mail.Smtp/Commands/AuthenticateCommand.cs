using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("AUTH")]
    public class AuthenticateCommand : BaseCommand
    {
        private readonly IIndex<string, IAuthenticationSession> _authentication;
        private readonly IMessageChannel _channel;

        public AuthenticateCommand(
			IIndex<string, IAuthenticationSession> authentication,
			IMessageChannel channel)
        {
            _authentication = authentication;
            _channel = channel;
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            string[] parts = Arguments.Split(new[] {' '}, 2);
            if (parts == null || parts.Length == 0 || parts.Length > 2)
            {
                await _channel.SendReplyAsync(
                    ReplyCode.InvalidArguments,
                    "Expected mechanism and optional initial response",
                    token);
                return;
            }
            string mechanismName = parts[0];
            IAuthenticationSession mechanism;

			if (!_authentication.TryGetValue(mechanismName, out mechanism))
            {
                await _channel.SendReplyAsync(ReplyCode.InvalidArguments, "Unknown mechanism", token);
                return;
            }

            UserData userData;

            try
            {
                userData = await mechanism.AuthenticateAsync(false, token);
            }
            catch (ArgumentException)
            {
                await _channel.SendReplyAsync(ReplyCode.InvalidArguments, "Invalid arguments", token);
                return;
            }

			_channel.AuthenticatedUser = userData;
            await _channel.SendReplyAsync(ReplyCode.AuthenticationComplete, "Authentication unsuccessful", token);
        }
    }
}