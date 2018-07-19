using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("FETCH", SessionState.Selected)]
	public class FetchCommand : BaseImapCommand
	{
		private IEnumerable<string> _fetchItems;
		private NumberRangeMessageData _messageSet;

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 2)
			{
				return false;
			}

			_messageSet = arguments[0] as NumberRangeMessageData;
			if (_messageSet == null)
			{
				return false;
			}

			var list = arguments[1] as ListMessageData;
			if (list == null || list.Items.Count == 0)
			{
				return false;
			}

			_fetchItems = ResolveAliases(list.Items.Select(i => MessageData.GetString(i, Encoding.UTF8)));
			return true;
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return true;
		}

		public override Task ExecuteAsync(CancellationToken cancellationToken)
		{
			IEnumerable<string> items = ResolveAliases(_fetchItems);

			throw new NotImplementedException();
		}

		private IEnumerable<string> ResolveAliases(IEnumerable<string> fetchItems)
		{
			return fetchItems.Select(ExpandAlias).SelectMany(s => s);
		}

		private IEnumerable<string> ExpandAlias(string item)
		{
			switch (item.ToUpperInvariant())
			{
				case "ALL":
					return new[] {"FLAGS", "INTERNALDATE", "RFC822.SIZE", "ENVELOPE"};
				case "FAST":
					return new[] {"FLAGS", "INTERNALDATE", "RFC822.SIZE"};
				case "FULL":
					return new[] {"FLAGS", "INTERNALDATE", "RFC822.SIZE ENVELOPE BODY"};
			}

			return new[] {item};
		}
	}
}
