using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Dispatcher
{
	public interface IMailBoxStore
	{
		Task<IMailWriteReference> NewMailAsync(string mailbox, CancellationToken token);

		IEnumerable<IMailReference> GetAllMailReferences();
		Task<IMailReadReference> OpenReadAsync(IMailReference reference);

		Task DeleteAsync(IMailReference reference);
	}
}