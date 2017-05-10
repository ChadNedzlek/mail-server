using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockUserStore : IUserStore
	{
		public bool Accept { get; }

		public MockUserStore(bool accept)
		{
			Accept = accept;
		}

		public Task<UserData> GetUserWithPasswordAsync(string userName, string password, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task<byte[]> GetSaltForUserAsync(string username, CancellationToken cancellationToken)
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