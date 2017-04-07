using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailTransferQueue
	{
		Task<IMailWriteReference> NewMailAsync(string sender, IImmutableList<string> recipients, CancellationToken token);
		IEnumerable<string> GetMailsByDomain();
		IEnumerable<IMailReference> GetAllMailForDomain(string domain);

		Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token);
		Task DeleteAsync(IMailReference reference);
	}
}