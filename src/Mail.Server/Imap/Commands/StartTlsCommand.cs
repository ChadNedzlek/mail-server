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

		public override async Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			if (session.Connection.IsEncrypted)
			{
				await session.SendMessageAsync(GetBadMessage("STARTTLS already complete"), cancellationToken);
				return;
			}

			await session.SendMessageAsync(GetOkMessage("STARTTLS completed, begin TLS negotiation"), cancellationToken);

			await session.Connection.NegotiateTlsAsync();

			await session.EndCommandWithoutResponseAsync(this, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}
	}
}