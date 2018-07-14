namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("EXAMINE", SessionState.Authenticated)]
	public class ExamineCommand : ExamineOrSelectCommand
	{
		public ExamineCommand(IImapMailStore mailstore, IImapMessageChannel channel, IImapMailboxPointer mailboxPointer) :
			base(mailstore, channel, mailboxPointer)
		{
		}

		public override bool IsExamine => true;
	}
}
