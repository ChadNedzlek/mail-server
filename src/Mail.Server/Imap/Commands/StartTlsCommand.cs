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
		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments) => arguments.Count == 0;

		private readonly SecurableConnection _connection;
		private readonly IImapMessageChannel _channel;

		public StartTlsCommand(SecurableConnection connection, IImapMessageChannel channel)
		{
			_connection = connection;
			_channel = channel;
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

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}
	}
}