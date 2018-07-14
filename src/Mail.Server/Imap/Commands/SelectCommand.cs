namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("SELECT", SessionState.Authenticated)]
	public class SelectCommand : ExamineOrSelectCommand
	{
		public SelectCommand(IImapMailStore mailstore, IImapMessageChannel channel, IImapMailboxPointer mailboxPointer) :
			base(mailstore, channel, mailboxPointer)
		{
		}

		public override bool IsExamine => false;
	}
}
