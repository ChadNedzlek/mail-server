using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("STARTTLS", SessionState.Open)]
	public class StartTlsCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;

		private readonly SecurableConnection _connection;

		public StartTlsCommand(SecurableConnection connection, IImapMessageChannel channel)
		{
			_connection = connection;
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return arguments.Count == 0;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			if (_connection.IsEncrypted)
			{
				await _channel.SendMessageAsync(GetBadMessage("STARTTLS already complete"), cancellationToken);
				return;
			}

			await _channel.SendMessageAsync(GetOkMessage("STARTTLS completed, begin TLS negotiation"), cancellationToken);

			await _connection.NegotiateTlsAsync();

			await _channel.EndCommandWithoutResponseAsync(this, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return false;
		}
	}
}
