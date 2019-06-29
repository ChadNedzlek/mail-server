using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IConnectionSecurity
	{
		bool CanEncrypt { get; }
		bool IsEncrypted { get; }
		X509Certificate2 GetCertificate();
	}
}
