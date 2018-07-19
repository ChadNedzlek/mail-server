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
		private readonly IImapMessageChannel _channel;

		public CheckCommand(IImapMessageChannel channel)
		{
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return arguments.Count == 0;
		}

		public override Task ExecuteAsync(CancellationToken cancellationToken)
		{
			return EndOkAsync(_channel, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return true;
		}
	}
}
