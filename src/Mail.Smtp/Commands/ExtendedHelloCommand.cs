using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailServer;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("EHLO")]
    public class ExtendedHelloCommand : BaseCommand
    {
        private readonly IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> _authentication;
        private readonly SecurableConnection _connection;
        private readonly IMessageChannel _channel;
        private readonly SmtpSettings _settings;

        private static readonly ImmutableList<string> s_generalExtensions = ImmutableList.CreateRange(
            new[]
            {
                "8BITMIME",
                "UTF8SMTP",
                "SMTPUTF8",
                "CHUNKING",
                "BINARYMIME",
            });

        private static readonly ImmutableList<string> s_plainTextExtensions = ImmutableList.CreateRange(
            new[]
            {
                "STARTTLS"
            });

        public ExtendedHelloCommand(
            IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> authentication,
			SecurableConnection connection,
			IMessageChannel channel,
			SmtpSettings settings)
        {
            _authentication = authentication;
            _connection = connection;
            _channel = channel;
            _settings = settings;
        }


        public override async Task ExecuteAsync(CancellationToken token)
        {
            ImmutableList<string> encryptedExtensions = ImmutableList.CreateRange(
                new[]
                {
                    "AUTH " + String.Join(" ", _authentication.Select(a => a.Metadata.Name)),
                });

			_channel.ConnectedHost = Arguments;
            ImmutableList<string> plainTextExtensions = s_plainTextExtensions;
            var extensions =
                s_generalExtensions.Concat(_connection.IsEncrypted ? encryptedExtensions : plainTextExtensions);

            if (_connection.Certificate != null && !_connection.IsEncrypted)
            {
                extensions = extensions.Concat(new[] {"STARTTLS"});
            }

            await _channel.SendReplyAsync(
                ReplyCode.Okay,
                true,
                $"{_settings.DomainName} greets {Arguments}",
                token);
            await _channel.SendReplyAsync(ReplyCode.Okay, extensions, token);
        }
    }
}