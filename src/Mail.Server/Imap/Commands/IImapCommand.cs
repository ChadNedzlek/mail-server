using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	public interface IImapCommand
	{
		string Tag { get; }
		string CommandName { get; }
		ImmutableList<IMessageData> Arguments { get; }
		bool HasValidArguments { get; }
		Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken);
		bool IsValidWith(IEnumerable<IImapCommand> commands);
	}
}