using System.Collections.Immutable;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("UNSUBSCRIBE", SessionState.Authenticated)]
	public class UnsubscribeCommand : SubscribeOrUnsubscribeCommand
	{
		public override bool IsSubscribe => false;

		public UnsubscribeCommand(IImapMessageChannel channel, IImapMailStore mailstore) : base(channel, mailstore)
		{
		}
	}
}