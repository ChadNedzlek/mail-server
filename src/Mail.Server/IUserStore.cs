using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IUserStore
	{
		Task<UserData> GetUserWithPasswordAsync(string userName, string password, CancellationToken cancellationToken);
		Task<byte[]> GetSaltForUserAsync(string username, CancellationToken cancellationToken);

		bool CanUserSendAs(UserData user, string mailBox);

	    Task AddUserAsync(string username, string password, CancellationToken token);
	}
}