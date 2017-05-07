using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("RENAME", SessionState.Authenticated)]
	public class RenameCommand : BaseImapCommand
	{
		private string _newMailbox;
		private string _oldMailbox;

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 2)
			{
				return false;
			}

			_oldMailbox = MessageData.GetString(arguments[0], Encoding.UTF8);
			_newMailbox = MessageData.GetString(arguments[1], Encoding.UTF8);

			if (string.IsNullOrEmpty(_oldMailbox) || string.IsNullOrEmpty(_newMailbox))
			{
				return false;
			}

			return true;
		}

		public override async Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			try
			{
				await session.MailStore.RenameMailboxAsync(session.AuthenticatedUser, _oldMailbox, _newMailbox, cancellationToken);
			}
			catch (Exception)
			{
				await EndWithResultAsync(session, CommandResult.No, "can't rename mailbox", cancellationToken);
				return;
			}

			await EndOkAsync(session, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}
	}
}