using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Imap
{
	public interface IImapMailStore
	{
		Task<Mailbox> GetMailBoxAsync(
			UserData authenticatedUser,
			string mailbox,
			bool isExamine,
			CancellationToken cancellationToken);

		Task<Mailbox> CreateMailboxAsync(UserData authenticatedUser, string mailbox, CancellationToken cancellationToken);
		Task DeleteMailboxAsync(UserData authenticatedUser, string mailbox, CancellationToken cancellationToken);

		Task RenameMailboxAsync(
			UserData authenticatedUser,
			string oldMailbox,
			string newMailbox,
			CancellationToken cancellationToken);

		Task MarkMailboxSubscribedAsync(
			UserData authenticatedUser,
			string mailbox,
			bool subscribed,
			CancellationToken cancellationToken);

		Task<IEnumerable<Mailbox>> ListMailboxesAsync(
			UserData authenticatedUser,
			string pattern,
			CancellationToken cancellationToken);

		Task SaveAsync(MailMessage message);
		Task RefreshAsync(MailMessage message);

		Task SaveBinaryAsync(
			string mailbox,
			DateTime dateTime,
			IEnumerable<string> flags,
			byte[] data,
			CancellationToken cancellationToken);
	}
}
