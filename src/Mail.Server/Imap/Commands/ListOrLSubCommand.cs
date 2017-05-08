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
		private readonly IImapMailStore _mailstore;
		private readonly IImapMessageChannel _channel;

		protected ListOrLSubCommand(IImapMessageChannel channel, IImapMailStore mailstore)
		{
			_channel = channel;
			_mailstore = mailstore;
		}

		public abstract bool IsLSub { get; }

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 2) return false;

			_reference = MessageData.GetString(arguments[0], Encoding.UTF8);
			_pattern = MessageData.GetString(arguments[0], Encoding.UTF8);

			return true;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(_reference) && string.IsNullOrEmpty(_pattern))
			{
				await _channel.SendMessageAsync(
					new Message(
						UntaggedTag,
						CommandName,
						new ListMessageData(new AtomMessageData(Tags.NoSelect)),
						new QuotedMessageData(Constants.HeirarchySeparator),
						new QuotedMessageData("")),
					cancellationToken);

				await EndOkAsync(_channel, cancellationToken);
				return;
			}

			IEnumerable<Mailbox> mailboxes =
				await _mailstore.ListMailboxesAsync(_channel.AuthenticatedUser, _reference + _pattern, cancellationToken);
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

				#warning Presumably we should use the list...

				await _channel.SendMessageAsync(
					new Message(
						UntaggedTag,
						CommandName,
						new ListMessageData(new AtomMessageData(Tags.NoSelect)),
						new QuotedMessageData(Constants.HeirarchySeparator),
						MessageData.CreateData(mailbox.FullName)),
					cancellationToken);
			}

			await EndOkAsync(_channel, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}
	}
}