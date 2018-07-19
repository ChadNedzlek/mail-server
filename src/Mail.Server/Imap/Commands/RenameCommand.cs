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
		private readonly IImapMessageChannel _channel;
		private readonly IImapMailStore _mailstore;
		private string _newMailbox;
		private string _oldMailbox;

		public RenameCommand(
			IImapMessageChannel channel,
			IImapMailStore mailstore)
		{
			_channel = channel;
			_mailstore = mailstore;
		}

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

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			try
			{
				await _mailstore.RenameMailboxAsync(_channel.AuthenticatedUser, _oldMailbox, _newMailbox, cancellationToken);
			}
			catch (Exception)
			{
				await EndWithResultAsync(_channel, CommandResult.No, "can't rename mailbox", cancellationToken);
				return;
			}

			await EndOkAsync(_channel, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return false;
		}
	}
}
