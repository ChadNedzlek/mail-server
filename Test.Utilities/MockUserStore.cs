using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockUserStore : IUserStore
	{
		public MockUserStore(bool accept)
		{
			Accept = accept;
		}

		public bool Accept { get; }

		public Task<UserData> GetUserWithPasswordAsync(string userName, string password, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public bool CanUserSendAs(UserData user, string mailbox)
		{
			return Accept;
		}

		public Task AddUserAsync(string username, string password, CancellationToken token)
		{
			throw new NotImplementedException();
		}
	}
}
