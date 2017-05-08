using System.Collections.Immutable;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LSUB", SessionState.Authenticated)]
	public class LSubCommand : ListOrLSubCommand
	{
		public override bool IsLSub => true;

		public LSubCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}
	}
}