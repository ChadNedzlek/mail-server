namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LSUB", SessionState.Authenticated)]
	public class LSubCommand : ListOrLSubCommand
	{
		public LSubCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}

		public override bool IsLSub => true;
	}
}
