using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Mail.FileSystem.Test
{
	public class HashedPasswordUserStore : IDisposable
	{
		public HashedPasswordUserStore()
		{
			string storePath = Path.GetTempFileName();
			_settings = TestHelpers.MakeSettings(domainName: "test.vaettir.net", userPasswordFile:storePath, passwordAlgorithm: "db:sha1:98374");
			_store = new Vaettir.Mail.Server.FileSystem.HashedPasswordUserStore(_settings);
		}

		public void Dispose()
		{
			File.Delete(_settings.UserPasswordFile);
		}

		private readonly AgentSettings _settings;
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
