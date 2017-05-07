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
		private Encoding _encoding;
		
		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return true;
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}

		public override Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			// TODO: Implement it
			return EndWithResultAsync(session, CommandResult.No, "SEARCH not supported", cancellationToken);
		}
	}
}