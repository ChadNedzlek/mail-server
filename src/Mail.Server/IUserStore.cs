using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IUserStore
	{
		Task<UserData> GetUserWithPasswordAsync(string userName, string password, CancellationToken cancellationToken);

		bool CanUserSendAs(UserData user, string mailbox);

		Task AddUserAsync(string username, string password, CancellationToken token);
	}
}
