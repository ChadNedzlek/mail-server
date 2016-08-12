using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IUserStore
	{
		Task<UserData> GetUserWithPasswordAsync(string userName, string password);
		Task<byte[]> GetSaltForUserAsync(string username, CancellationToken cancellationToken);
	}
}