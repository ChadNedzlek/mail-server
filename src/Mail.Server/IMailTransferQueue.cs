using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailTransferQueue : IMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string id, string sender, IImmutableList<string> recipients, CancellationToken token);
		IEnumerable<string> GetAllPendingDomains();
		IEnumerable<IMailReference> GetAllMailForDomain(string domain);
	}
}