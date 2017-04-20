using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailQueue : IMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string sender, IImmutableList<string> recipients, CancellationToken token);
		IEnumerable<IMailReference> GetAllMailReferences();
	}
}
