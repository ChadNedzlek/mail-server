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

		public CloseCommand(IImapMessageChannel channel)
		{
			_channel = channel;
		}

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

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			ExpungeAllDeletedMessages(_channel);
			session.DiscardPendingExpungeResponses();
			await session.UnselectMailboxAsync(cancellationToken);
			await EndOkAsync(_channel, "CLOSE completed, now in authenticated state", cancellationToken);
		}

		private void ExpungeAllDeletedMessages(IImapMessageChannel session)
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