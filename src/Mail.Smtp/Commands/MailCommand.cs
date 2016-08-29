using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class MailCommand : ICommandFactory
	{
		public string Name => "MAIL";

		public ICommand CreateCommand(string arguments)
		{
			return new Implementation(Name, arguments);
		}

		private class Implementation : BaseCommand
		{
			public Implementation(string name, string arguments) : base(name, arguments)
			{
			}

			private static readonly Regex FromExpression = new Regex(@"^FROM:<([^:+]:)?(\S*)>(?: (.+))?$");
			public override Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token)
			{
				if (smtpSession.PendingMail != null)
				{
					return smtpSession.SendReplyAsync(ReplyCode.BadSequence, "MAIL not allowed now", CancellationToken.None);
				}

				Match fromMatch = FromExpression.Match(Arguments);
				if (!fromMatch.Success)
				{
					return smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Bad FROM address", CancellationToken.None);
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
				if (!TryProcessParameterValue(smtpSession, parameterText, out errorReport, token))
				{
					return errorReport;
				}

				if (smtpSession.IsAuthenticated &&
					!smtpSession.UserStore.CanUserSendAs(smtpSession.AuthenticatedUser, mailBox))
				{
					return smtpSession.SendReplyAsync(ReplyCode.MailboxUnavailable, "Invalid mailbox", token);
				}

				if (!smtpSession.IsAuthenticated &&
					smtpSession.Settings.RelayDomains.Contains(MailUtilities.GetDomainFromMailbox(mailBox)))
				{
					return smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Must be signed in to send from domain", token);
				}

				smtpSession.PendingMail = new SmtpMailMessage(
					new SmtpPath(
						sourceRouteList,
						mailBox));

				return smtpSession.SendReplyAsync(ReplyCode.Okay, token);
			}

			protected override bool TryProcessParameter(SmtpSession session, string key, string value)
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
								session.PendingMail.IsBinary = true;
								return true;
						}
						return false;
					default:
						return base.TryProcessParameter(session, key, value);
				}
			}
		}
	}
}