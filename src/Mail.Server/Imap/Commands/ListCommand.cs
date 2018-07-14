namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LIST", SessionState.Authenticated)]
	public class ListCommand : ListOrLSubCommand
	{
		public ListCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}

		public override bool IsLSub => false;
	}
}
