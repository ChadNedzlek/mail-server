using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IProtocolSession
	{
		string Id { get; }
		Task RunAsync(CancellationToken cancellationToken);
	}
}
