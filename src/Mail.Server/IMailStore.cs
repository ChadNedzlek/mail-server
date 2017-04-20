using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailStore : IWriter
	{
		Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token);
		Task DeleteAsync(IMailReference reference);
	}
}
