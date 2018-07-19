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
		Task ExecuteAsync(CancellationToken cancellationToken);
		bool IsValidWith(IReadOnlyList<IImapCommand> commands);
		void Initialize(string commandName, string tag, ImmutableList<IMessageData> data);
	}
}
