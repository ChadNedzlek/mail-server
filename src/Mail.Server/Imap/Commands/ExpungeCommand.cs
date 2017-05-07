using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("EXPUNGE", SessionState.Selected)]
	public class ExpungeCommand : BaseImapCommand
	{

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return arguments.Count == 0;
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			foreach (IImapCommand command in commands)
			{
				switch (command.CommandName)
				{
					case "EXPUNGE":
					case "FETCH":
					case "SEARCH":
					case "STORE":
					case "COPY":
					case "UID":
						return false;
				}
			}
			return true;
		}

		public override Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			ExpungeAllDeletedMessages(session);
			return EndOkAsync(session, cancellationToken);
		}

		private void ExpungeAllDeletedMessages(ImapSession session)
		{
			Mailbox mailbox = session.SelectedMailbox.Mailbox;
			foreach (MailMessage message in mailbox.Messages)
			{
				if (message.Flags.Contains(Tags.Deleted))
				{
					mailbox.Messages.Remove(message);
				}
			}
		}
	}
}