using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("MAIL")]
	public class MailCommand : BaseSmtpCommand
	{
		private static readonly Regex s_fromExpression = new Regex(@"^FROM:<([^:]+:)?(\S*)>(?: (.+))?$");
		private readonly IMailBuilder _builder;
		private readonly ISmtpMessageChannel _channel;
		private readonly AgentSettings _settings;
		private readonly IUserStore _userStore;

		public MailCommand(ISmtpMessageChannel channel, IMailBuilder builder, AgentSettings settings, IUserStore userStore)
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
				return _channel.SendReplyAsync(SmtpReplyCode.BadSequence, "MAIL not allowed now", CancellationToken.None);
			}

			Match fromMatch = s_fromExpression.Match(Arguments);
			if (!fromMatch.Success)
			{
				return _channel.SendReplyAsync(
					SmtpReplyCode.InvalidArguments,
					"Bad FROM address",
					CancellationToken.None);
			}

			string sourceRoute = fromMatch.Groups[1].Value;
			string mailbox = fromMatch.Groups[2].Value;
			string parameterText = fromMatch.Groups[3].Value;

			ImmutableList<string> sourceRouteList = null;
			if (!string.IsNullOrEmpty(sourceRoute))
			{
				return _channel.SendReplyAsync(
					SmtpReplyCode.InvalidArguments,
					"Return path not supported",
					CancellationToken.None);

			}

			Task errorReport;
			if (!TryProcessParameterValue(_channel, parameterText, out errorReport, token))
			{
				return errorReport;
			}

			if (_channel.IsAuthenticated &&
				!_userStore.CanUserSendAs(_channel.AuthenticatedUser, mailbox))
			{
				return _channel.SendReplyAsync(SmtpReplyCode.MailboxUnavailable, "Invalid Mailbox", token);
			}

			if (!_channel.IsAuthenticated &&
				_settings.LocalDomains?.Any(
					d => string.Equals(
						d.Name,
						MailUtilities.GetDomainFromMailbox(mailbox),
						StringComparison.OrdinalIgnoreCase)
					) == true
				)
			{
				return _channel.SendReplyAsync(
					SmtpReplyCode.InvalidArguments,
					"Must be signed in to send from domain",
					token);
			}

			_builder.PendingMail = new SmtpMailMessage(
				new SmtpPath(
					mailbox));

			return _channel.SendReplyAsync(SmtpReplyCode.Okay, token);
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
					}
					return false;
				default:
					return base.TryProcessParameter(key, value);
			}
		}
	}
}
