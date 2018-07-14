namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("UNSUBSCRIBE", SessionState.Authenticated)]
	public class UnsubscribeCommand : SubscribeOrUnsubscribeCommand
	{
		public UnsubscribeCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}

		public override bool IsSubscribe => false;
	}
}
