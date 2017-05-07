using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("CHECK", SessionState.Selected)]
	public class CheckCommand : BaseImapCommand
	{
		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments) => arguments.Count == 0;

		public override Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			return EndOkAsync(session, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}
	}
}