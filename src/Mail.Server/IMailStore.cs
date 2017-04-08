using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailStore
	{
		Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token);
		Task SaveAsync(IMailWriteReference reference, CancellationToken token);
		Task DeleteAsync(IMailReference reference);
	}
}