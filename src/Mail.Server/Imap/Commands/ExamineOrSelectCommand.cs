using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	public abstract class ExamineOrSelectCommand : BaseImapCommand
	{
		private string _mailbox;

		public abstract bool IsExamine { get; }

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 1)
			{
				return false;
			}

			_mailbox = MessageData.GetString(arguments[0], Encoding.UTF8);
			return true;
		}

		public override async Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			Mailbox mailbox =
				await session.MailStore.GetMailBoxAsync(session.AuthenticatedUser, _mailbox, IsExamine, cancellationToken);

			if (mailbox == null)
			{
				await EndWithResultAsync(session, CommandResult.No, "no such mailbox", cancellationToken);
				return;
			}

			if (!mailbox.IsSelectable)
			{
				await EndWithResultAsync(session, CommandResult.No, "cannot select mailbox", cancellationToken);
				return;
			}

			SelectedMailbox selected = await session.SelectMailboxAsync(mailbox, cancellationToken);

			await session.SendMessageAsync(
				new Message(UntaggedTag, new NumberMessageData(mailbox.Messages.Count), new AtomMessageData("EXISTS")),
				CancellationToken.None);

			await session.SendMessageAsync(
				new Message(UntaggedTag, new NumberMessageData(mailbox.Recent.Count), new AtomMessageData("RECENT")),
				CancellationToken.None);

			int unseenSequence = mailbox.FirstUnseen;
			int uidNext = mailbox.NextUid;
			int uidValidity = mailbox.UidValidity;

			await session.SendMessageAsync(
				new Message(
					UntaggedTag,
					"OK",
					new TagMessageData(
						new AtomMessageData("UNSEEN"),
						new NumberMessageData(unseenSequence)),
					new ServerMessageData($"Messsage {unseenSequence} is the first unseen")),
				cancellationToken);

			await session.SendMessageAsync(
				new Message(
					UntaggedTag,
					"OK",
					new TagMessageData(
						new AtomMessageData("UIDVALIDITY"),
						new NumberMessageData(uidValidity)),
					new ServerMessageData("UIDs valied")),
				cancellationToken);

			await session.SendMessageAsync(
				new Message(
					UntaggedTag,
					"OK",
					new TagMessageData(
						new AtomMessageData("UIDNEXT"),
						new NumberMessageData(uidNext)),
					new ServerMessageData("Predicted next UID")),
				cancellationToken);

			await session.SendMessageAsync(
				new Message(
					UntaggedTag,
					"FLAGS",
					new ListMessageData(mailbox.Flags.Select(f => new AtomMessageData(f)))),
				cancellationToken);

			await session.SendMessageAsync(
				new Message(
					UntaggedTag,
					"OK",
					new TagMessageData(
						new AtomMessageData("PERMANENTFLAGS"),
						new ListMessageData(selected.PermanentFlags.Select(f => new AtomMessageData(f))))),
				cancellationToken);

			string readWrite = mailbox.IsReadOnly ? "READ-ONLY" : "READ-WRITE";

			await
				EndWithResultAsync(
					session,
					CommandResult.Ok,
					new TagMessageData(new AtomMessageData(readWrite)),
					null,
					cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}
	}
}