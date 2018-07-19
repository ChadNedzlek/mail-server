using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("CLOSE", SessionState.Selected)]
	public class CloseCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;
		private readonly IImapMailboxPointer _mailboxPointer;

		public CloseCommand(IImapMessageChannel channel, IImapMailboxPointer mailboxPointer)
		{
			_channel = channel;
			_mailboxPointer = mailboxPointer;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return arguments.Count == 0;
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
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

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			ExpungeAllDeletedMessages();
			_channel.DiscardPendingExpungeResponses();
			await _mailboxPointer.UnselectMailboxAsync(cancellationToken);
			await EndOkAsync(_channel, "CLOSE completed, now in authenticated state", cancellationToken);
		}

		private void ExpungeAllDeletedMessages()
		{
			Mailbox mailbox = _mailboxPointer.SelectedMailbox.Mailbox;
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
