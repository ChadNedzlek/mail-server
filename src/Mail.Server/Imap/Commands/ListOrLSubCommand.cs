using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	public abstract class ListOrLSubCommand : BaseImapCommand
	{
		private string _pattern;
		private string _reference;

		public abstract bool IsLSub { get; }

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 2) return false;

			_reference = MessageData.GetString(arguments[0], Encoding.UTF8);
			_pattern = MessageData.GetString(arguments[0], Encoding.UTF8);

			return true;
		}

		public override async Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(_reference) && string.IsNullOrEmpty(_pattern))
			{
				await session.SendMessageAsync(
					new Message(
						UntaggedTag,
						CommandName,
						new ListMessageData(new AtomMessageData(Tags.NoSelect)),
						new QuotedMessageData(Constants.HeirarchySeparator),
						new QuotedMessageData("")),
					cancellationToken);

				await EndOkAsync(session, cancellationToken);
				return;
			}

			IEnumerable<Mailbox> mailboxes =
				await session.MailStore.ListMailboxesAsync(session.AuthenticatedUser, _reference + _pattern, cancellationToken);
			foreach (Mailbox mailbox in mailboxes)
			{
				ListMessageData list;
				if (mailbox.IsSelectable)
				{
					list = new ListMessageData();
				}
				else
				{
					list = new ListMessageData(new AtomMessageData(Tags.NoSelect));
				}

				await session.SendMessageAsync(
					new Message(
						UntaggedTag,
						CommandName,
						new ListMessageData(new AtomMessageData(Tags.NoSelect)),
						new QuotedMessageData(Constants.HeirarchySeparator),
						MessageData.CreateData(mailbox.FullName)),
					cancellationToken);
			}

			await EndOkAsync(session, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}
	}
}