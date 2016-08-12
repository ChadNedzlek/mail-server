using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[CommandFactory]
	public class RecipientCommand : ICommandFactory
	{
		public string Name => "RCPT";
		public ICommand CreateCommand(string arguments)
		{
			return new Implementation(Name, arguments);
		}

		private class Implementation : BaseCommand
		{
			public Implementation(string name, string arguments) : base(name, arguments)
			{
			}

			private static readonly Regex FromExpression = new Regex(@"^TO:<([^:+]:)?(\S*)(?: (.*))?>$");
			public override Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token)
			{
				if (smtpSession.PendingMail == null)
				{
					return smtpSession.SendReplyAsync(ReplyCode.BadSequence, "RCPT not valid now", token);
				}

				Match toMatch = FromExpression.Match(Arguments);
				if (!toMatch.Success)
				{
					return smtpSession.SendReplyAsync(ReplyCode.InvalidArguments, "Bad FROM address", token);
				}

				string sourceRoute = toMatch.Groups[1].Value;
				string mailBox = toMatch.Groups[2].Value;

				if (!String.IsNullOrEmpty(sourceRoute))
				{
					return smtpSession.SendReplyAsync(ReplyCode.NameNotAllowed, "Forwarding not supported", token);
				}

				string parameterString = toMatch.Groups[3].Value;

				Task errorReport;
				if (!TryProcessParameterValue(smtpSession, parameterString, out errorReport, token))
				{
					return errorReport;
				}

				smtpSession.PendingMail.Recipents.Add(mailBox);

				return smtpSession.SendReplyAsync(ReplyCode.Okay, token);
			}
		}
	}
}