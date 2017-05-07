namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LIST", SessionState.Authenticated)]
	public class ListCommand : ListOrLSubCommand
	{
		public override bool IsLSub => false;
	}
}