using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Imap
{
	public class SelectedMailbox
	{
		private readonly LinkedList<int> _pendingExpungedMails = new LinkedList<int>();

		public SelectedMailbox(Mailbox mailbox)
		{
			Mailbox = mailbox;
		}

		public Mailbox Mailbox { get; }
		public ImmutableList<string> PermanentFlags { get; private set; }

		public Task<MailMessage> GetItemBySequenceAsync(int sequenceNumber)
		{
			throw new NotImplementedException();
		}
	}
}