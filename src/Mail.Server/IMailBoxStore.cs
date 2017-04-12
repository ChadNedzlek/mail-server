using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailBoxStore : IMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string mailbox, string folder, CancellationToken token);
		Task MoveAsync(IMailReference reference, string folder, CancellationToken token);
		Task SetFlags(IMailReference reference, IEnumerable<string> flags, CancellationToken token);
	}

	public static class MailBoxStoreExtensions
	{
		public static Task<IMailWriteReference> NewMailAsync(
			this IMailBoxStore store,
			string mailbox,
			CancellationToken token)
		{
			return store.NewMailAsync(mailbox, "inbox", token);
		}
	}
}