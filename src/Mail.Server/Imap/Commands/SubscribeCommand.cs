using System.Collections.Immutable;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("SUBSCRIBE", SessionState.Authenticated)]
	public class SubscribeCommand : SubscribeOrUnsubscribeCommand
	{
		public override bool IsSubscribe => true;

		public SubscribeCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}
	}
}