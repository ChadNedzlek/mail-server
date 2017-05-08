using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Imap
{
	public interface IImapMailboxPointer
	{
		SelectedMailbox SelectedMailbox { get; }
		Task<SelectedMailbox> SelectMailboxAsync(Mailbox mailbox, CancellationToken cancellationToken);
		Task UnselectMailboxAsync(CancellationToken cancellationToken);
	}
}