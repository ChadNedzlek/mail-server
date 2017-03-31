using System.Security.Cryptography.X509Certificates;

namespace Vaettir.Mail.Server
{
    public interface IConnectionSecurity
    {
        X509Certificate2 Certificate { get; set; }
        bool IsEncrypted { get; }
    }
}