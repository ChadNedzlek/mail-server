using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IProtocolSession
	{
		Task Start(CancellationToken cancellationToken);
		Task CloseAsync(CancellationToken cancellationToken);
	}
}