using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("NOOP", SessionState.Open)]
	public class NoopCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;

		public NoopCommand(IImapMessageChannel channel)
		{
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return arguments.Count == 0;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			await EndOkAsync(_channel, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return true;
		}
	}
}
