using System.Collections.Immutable;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("SELECT", SessionState.Authenticated)]
	public class SelectCommand : ExamineOrSelectCommand
	{
		public override bool IsExamine => false;
	}
}