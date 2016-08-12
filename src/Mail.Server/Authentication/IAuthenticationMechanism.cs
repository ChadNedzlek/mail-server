using Vaettir.Utility;

namespace Vaettir.Mail.Server.Authentication
{
	public interface IAuthenticationMechanism : IFactory
	{
		bool RequiresEncryption { get; }
		IAuthenticationSession CreateSession(IAuthenticationTransport session);
	}
}