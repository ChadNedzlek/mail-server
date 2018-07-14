using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("SEARCH", SessionState.Selected)]
	public class SearchCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;
		private Encoding _encoding;

		public SearchCommand(IImapMessageChannel channel)
		{
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return true;
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}

		public override Task ExecuteAsync(CancellationToken cancellationToken)
		{
			// TODO: Implement it
			return EndWithResultAsync(_channel, CommandResult.No, "SEARCH not supported", cancellationToken);
		}
	}
}
