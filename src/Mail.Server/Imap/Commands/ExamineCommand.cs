namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("EXAMINE", SessionState.Authenticated)]
	public class ExamineCommand : ExamineOrSelectCommand
	{
		public override bool IsExamine => true;

		public ExamineCommand(IImapMailStore mailstore, IImapMessageChannel channel) : base(mailstore, channel)
		{
		}
	}
}