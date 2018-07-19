using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("DELETE", SessionState.Authenticated)]
	public class DeleteCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;

		private readonly IImapMailStore _mailstore;
		private string _mailbox;

		public DeleteCommand(IImapMailStore mailstore, IImapMessageChannel channel)
		{
			_mailstore = mailstore;
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 1)
			{
				return false;
			}

			_mailbox = MessageData.GetString(arguments[0], Encoding.UTF8);
			if (string.IsNullOrEmpty(_mailbox))
			{
				return false;
			}

			return true;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			try
			{
				await _mailstore.DeleteMailboxAsync(_channel.AuthenticatedUser, _mailbox, cancellationToken);
			}
			catch (Exception)
			{
				await EndWithResultAsync(_channel, CommandResult.No, "failed to delete mailbox", cancellationToken);
				return;
			}

			await EndOkAsync(_channel, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return true;
		}
	}
}
