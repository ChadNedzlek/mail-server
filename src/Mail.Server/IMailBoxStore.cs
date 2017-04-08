using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailBoxStore : IMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string mailbox, CancellationToken token);
	}
}