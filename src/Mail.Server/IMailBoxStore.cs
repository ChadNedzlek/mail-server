using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailBoxStore
	{
		Task<IMailWriteReference> NewMailAsync(string mailbox, CancellationToken token);
	}
}