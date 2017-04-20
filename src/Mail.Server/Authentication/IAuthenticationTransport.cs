using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Authentication
{
	public interface IAuthenticationTransport
	{
		Task SendAuthenticationFragmentAsync(byte[] data, CancellationToken cancellationToken);
		Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken);
	}
}
