using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailTransferQueue
	{
		Task<IMailWriteReference> NewMailAsync(IEnumerable<string> recipients, string sender, CancellationToken token);
		IEnumerable<string> GetMailsByDomain();
		IEnumerable<IMailReference> GetAllMailForDomain(string domain);

		Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token);
		Task DeleteAsync(IMailReference reference);
	}
}