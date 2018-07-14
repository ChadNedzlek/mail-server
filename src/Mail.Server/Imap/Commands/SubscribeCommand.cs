namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("SUBSCRIBE", SessionState.Authenticated)]
	public class SubscribeCommand : SubscribeOrUnsubscribeCommand
	{
		public SubscribeCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}

		public override bool IsSubscribe => true;
	}
}
