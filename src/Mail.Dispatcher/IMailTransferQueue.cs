using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Dispatcher
{
	public interface IMailTransferQueue
	{
		Task<IMailWriteReference> NewMailAsync(IEnumerable<string> recipients, string sender, CancellationToken token);
	}
}