using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[Command("RCPT")]
	public class RecipientCommand : BaseCommand
	{
		private static readonly Regex s_fromExpression = new Regex(@"^TO:<([^:+]:)?(\S*)(?: (.*))?>$");

		private readonly IMailBuilder _builder;
		private readonly IMessageChannel _channel;
		private readonly SmtpSettings _settings;

		public RecipientCommand(IMailBuilder builder, IMessageChannel channel, SmtpSettings settings)
		{
			_builder = builder;
			_channel = channel;
			_settings = settings;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			if (_builder.PendingMail == null)
			{
				return _channel.SendReplyAsync(ReplyCode.BadSequence, "RCPT not valid now", token);
			}

			Match toMatch = s_fromExpression.Match(Arguments);
			if (!toMatch.Success)
			{
				return _channel.SendReplyAsync(ReplyCode.InvalidArguments, "Bad FROM address", token);
			}

			string sourceRoute = toMatch.Groups[1].Value;
			string mailbox = toMatch.Groups[2].Value;

			if (!string.IsNullOrEmpty(sourceRoute))
			{
				return _channel.SendReplyAsync(ReplyCode.NameNotAllowed, "Forwarding not supported", token);
			}

			string parameterString = toMatch.Groups[3].Value;

			Task errorReport;
			if (!TryProcessParameterValue(_channel, parameterString, out errorReport, token))
			{
				return errorReport;
			}

			string[] mailboxParts = mailbox.Split('@');
			if (mailboxParts.Length != 2)
			{
				return _channel.SendReplyAsync(ReplyCode.InvalidArguments, "Invalid Mailbox name", token);
			}

			string domain = mailboxParts[1];

			if (!_channel.IsAuthenticated &&
				_settings.RelayDomains?.Any(d => string.Equals(d.Name, domain, StringComparison.OrdinalIgnoreCase)) != true)
			{
				return _channel.SendReplyAsync(ReplyCode.MailboxUnavailable, "Invalid Mailbox", token);
			}

			_builder.PendingMail.Recipents.Add(mailbox);

			return _channel.SendReplyAsync(ReplyCode.Okay, token);
		}
	}
}
