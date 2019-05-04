using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IConnectionSecurity
	{
		bool CanEncrypt { get; }
		bool IsEncrypted { get; }
		Task<X509Certificate2> GetCertificateAsync(CancellationToken token);
	}
}
