using System;
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
	[ImapCommand("STATUS", SessionState.Authenticated)]
	public class StatusCommand : BaseImapCommand
	{
		private static readonly string[] ValidStatusItems =
		{
			"MESSAGES",
			"RECENT",
			"UIDNEXT",
			"UIDVALIDITY",
			"UNSEEN"
		};

		private readonly IImapMessageChannel _channel;
		private readonly IImapMailStore _mailstore;

		private ImmutableList<string> _items;
		private string _mailbox;

		public StatusCommand(IImapMessageChannel channel, IImapMailStore mailstore)
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

			_mailbox = MessageData.GetString(arguments[0], Encoding.UTF8);

			if (string.IsNullOrEmpty(_mailbox))
			{
				return false;
			}

			var itemList = arguments[1] as ListMessageData;

			if (itemList == null)
			{
				return false;
			}

			_items = ImmutableList.CreateRange(itemList.Items.Select(i => MessageData.GetString(i, Encoding.UTF8)));

			if (!_items.All(i => ValidStatusItems.Contains(i, StringComparer.Ordinal)))
			{
				return false;
			}

			return true;
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			Mailbox mailbox =
				await _mailstore.GetMailBoxAsync(_channel.AuthenticatedUser, _mailbox, true, cancellationToken);

			if (mailbox == null)
			{
				await EndWithResultAsync(_channel, CommandResult.No, "no mailbox with that name", cancellationToken);
				return;
			}

			var messageData = new List<IMessageData>();

			if (_items.Contains("MESSAGES"))
			{
				messageData.Add(new AtomMessageData("MESSAGES"));
				messageData.Add(new NumberMessageData(mailbox.Messages.Count));
			}

			if (_items.Contains("RECENT"))
			{
				messageData.Add(new AtomMessageData("RECENT"));
				messageData.Add(new NumberMessageData(mailbox.Recent.Count));
			}

			if (_items.Contains("UIDNEXT"))
			{
				messageData.Add(new AtomMessageData("UIDNEXT"));
				messageData.Add(new NumberMessageData(mailbox.NextUid));
			}

			if (_items.Contains("UIDVALIDITY"))
			{
				messageData.Add(new AtomMessageData("UIDVALIDITY"));
				messageData.Add(new NumberMessageData(mailbox.UidValidity));
			}

			if (_items.Contains("UNSEEN"))
			{
				messageData.Add(new AtomMessageData("UNSEEN"));
				messageData.Add(new NumberMessageData(mailbox.Messages.Count(m => m.Flags.Contains(Tags.Seen))));
			}

			await _channel.SendMessageAsync(
				new ImapMessage(UntaggedTag, CommandName, new ListMessageData(messageData)),
				cancellationToken);

			await EndOkAsync(_channel, cancellationToken);
		}
	}
}
