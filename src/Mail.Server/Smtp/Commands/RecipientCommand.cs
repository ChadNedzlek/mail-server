using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("RCPT")]
	public class RecipientCommand : BaseSmtpCommand
	{
		private static readonly Regex s_fromExpression = new Regex(@"^TO:\s*<([^:]+:)?(\S*)(?: (.*))?>$");

		private readonly IMailBuilder _builder;
		private readonly ISmtpMessageChannel _channel;
		private readonly AgentSettings _settings;

		public RecipientCommand(IMailBuilder builder, ISmtpMessageChannel channel, AgentSettings settings)
		{
			_builder = builder;
			_channel = channel;
			_settings = settings;
		}

		public override Task ExecuteAsync(CancellationToken token)
		{
			if (_builder.PendingMail == null)
			{
				return _channel.SendReplyAsync(SmtpReplyCode.BadSequence, "RCPT not valid now", token);
			}

			Match toMatch = s_fromExpression.Match(Arguments);
			if (!toMatch.Success)
			{
				return _channel.SendReplyAsync(SmtpReplyCode.InvalidArguments, "Bad FROM address", token);
			}

			string sourceRoute = toMatch.Groups[1].Value;
			string mailbox = toMatch.Groups[2].Value;

			if (!string.IsNullOrEmpty(sourceRoute))
			{
				return _channel.SendReplyAsync(SmtpReplyCode.NameNotAllowed, "Forwarding not supported", token);
			}

			string parameterString = toMatch.Groups[3].Value;

			if (!TryProcessParameterValue(_channel, parameterString, out Task errorReport, token))
			{
				return errorReport;
			}

			string[] mailboxParts = mailbox.Split('@');
			if (mailboxParts.Length != 2)
			{
				return _channel.SendReplyAsync(SmtpReplyCode.InvalidArguments, "Invalid Mailbox name", token);
			}

			string domain = mailboxParts[1];

			if (!_channel.IsAuthenticated &&
				_settings.RelayDomains?.Any(d => string.Equals(d.Name, domain, StringComparison.OrdinalIgnoreCase)) != true &&
				_settings.LocalDomains?.Any(d => string.Equals(d.Name, domain, StringComparison.OrdinalIgnoreCase)) != true)
			{
				return _channel.SendReplyAsync(SmtpReplyCode.MailboxUnavailable, "Invalid Mailbox", token);
			}

			_builder.PendingMail.Recipents.Add(mailbox);

			return _channel.SendReplyAsync(SmtpReplyCode.Okay, token);
		}
	}
}
