using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LOGOUT", SessionState.Open)]
	public class LogoutCommand : BaseImapCommand
	{

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments) => arguments.Count == 0;

		public override async Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			await session.SendMessageAsync(
				new Message(
					UntaggedTag,
					"BYE",
					new AtomMessageData("IMAP4rev1"),
					new ServerMessageData("Server logging out")),
				cancellationToken);

			await EndOkAsync(session, cancellationToken);
			session.EndSession();
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}
	}
}