using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("MAIL")]
    public class MailCommand : BaseCommand
    {
        private static readonly Regex s_fromExpression = new Regex(@"^FROM:<([^:+]:)?(\S*)>(?: (.+))?$");
        private readonly IMailBuilder _builder;
        private readonly IMessageChannel _channel;
        private readonly SmtpSettings _settings;
        private readonly IUserStore _userStore;

        public MailCommand(IMessageChannel channel, IMailBuilder builder, SmtpSettings settings, IUserStore userStore)
        {
            _builder = builder;
            _channel = channel;
            _settings = settings;
            _userStore = userStore;
        }

        public override Task ExecuteAsync(CancellationToken token)
        {
            if (_builder.PendingMail != null)
            {
                return _channel.SendReplyAsync(ReplyCode.BadSequence, "MAIL not allowed now", CancellationToken.None);
            }

            Match fromMatch = s_fromExpression.Match(Arguments);
            if (!fromMatch.Success)
            {
                return _channel.SendReplyAsync(
                    ReplyCode.InvalidArguments,
                    "Bad FROM address",
                    CancellationToken.None);
            }

            string sourceRoute = fromMatch.Groups[1].Value;
            string mailBox = fromMatch.Groups[2].Value;
            string parameterText = fromMatch.Groups[3].Value;

            ImmutableList<string> sourceRouteList = null;
            if (!String.IsNullOrEmpty(sourceRoute))
            {
                sourceRouteList = ImmutableList.CreateRange(sourceRoute.Split(','));
            }

            Task errorReport;
            if (!TryProcessParameterValue(_channel, parameterText, out errorReport, token))
            {
                return errorReport;
            }

            if (_channel.IsAuthenticated &&
                !_userStore.CanUserSendAs(_channel.AuthenticatedUser, mailBox))
            {
                return _channel.SendReplyAsync(ReplyCode.MailboxUnavailable, "Invalid mailbox", token);
            }

            if (!_channel.IsAuthenticated &&
                _settings.RelayDomains.Contains(MailUtilities.GetDomainFromMailbox(mailBox)))
            {
                return _channel.SendReplyAsync(
                    ReplyCode.InvalidArguments,
                    "Must be signed in to send from domain",
                    token);
            }

            _builder.PendingMail = new SmtpMailMessage(
                new SmtpPath(
                    sourceRouteList,
                    mailBox));

            return _channel.SendReplyAsync(ReplyCode.Okay, token);
        }

        protected override bool TryProcessParameter(string key, string value)
        {
            switch (key.ToUpperInvariant())
            {
                case "BODY":
                    switch (value.ToUpperInvariant())
                    {
                        case "7BIT":
                        case "8BITMIME":
                            return true;
                        case "BINARYMIME":
                            _builder.PendingMail.IsBinary = true;
                            return true;
                    }
                    return false;
                default:
                    return base.TryProcessParameter(key, value);
            }
        }
    }
}