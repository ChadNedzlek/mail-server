using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Authentication
{
	public interface IAuthenticationSession
	{
		Task<UserData> AuthenticateAsync(bool hasInitialResponse, CancellationToken token);
	}
}
