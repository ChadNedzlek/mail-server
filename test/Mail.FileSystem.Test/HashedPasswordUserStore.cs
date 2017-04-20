using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Xunit;

namespace Mail.FileSystem.Test
{
	public class HashedPasswordUserStore : IDisposable
	{
		public HashedPasswordUserStore()
		{
			string storePath = Path.GetTempFileName();
			_settings = new ProtocolSettings(null, "test.vaettir.net", null, storePath, "db:sha1:98374");
			_store = new Vaettir.Mail.Server.FileSystem.HashedPasswordUserStore(_settings);
		}

		public void Dispose()
		{
			File.Delete(_settings.UserPasswordFile);
		}

		private readonly ProtocolSettings _settings;
		private readonly Vaettir.Mail.Server.FileSystem.HashedPasswordUserStore _store;

		[Fact]
		public async Task AddAndGetUser()
		{
			await _store.AddUserAsync("testuser", "testpassword", CancellationToken.None);
			UserData user = await _store.GetUserWithPasswordAsync("testuser", "testpassword", CancellationToken.None);
			Assert.Equal("testuser", user.Mailbox);
		}

		[Fact]
		public async Task BaddPassword()
		{
			await _store.AddUserAsync("testuser", "testpassword", CancellationToken.None);
			UserData user = await _store.GetUserWithPasswordAsync("testuser", "wrongpassword", CancellationToken.None);
			Assert.Null(user);
		}

		[Fact]
		public async Task MissingUser()
		{
			await _store.AddUserAsync("testuser", "testpassword", CancellationToken.None);
			UserData user = await _store.GetUserWithPasswordAsync("nosuchuser", "testpassword", CancellationToken.None);
			Assert.Null(user);
		}
	}
}
