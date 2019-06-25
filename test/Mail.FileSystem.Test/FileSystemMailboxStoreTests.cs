using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.FileSystem.Test
{
	public class FileSystemMailboxStoreTests : IDisposable
	{
		private AgentSettings _settings;
		private FileSystemMailboxStore _store;
		private string _storePath;

		public FileSystemMailboxStoreTests()
		{
			_storePath = Path.GetTempFileName();
			File.Delete(_storePath);
			_settings = TestHelpers.MakeSettings(
				domainName: "example.com",
				localDomains: new SmtpAcceptDomain[] {new SmtpAcceptDomain("example.com")},
				mailLocalPath: _storePath
			);
			_store = new FileSystemMailboxStore(_settings, new MockLogger());
		}

		[Fact]
		public async Task DroppedMailTest()
		{
			using (var newMail = await _store.NewMailAsync("test-id", "test@example.com", CancellationToken.None))
			{
			}
			
			Assert.Empty(Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories));
		}


		[Fact]
		public async Task DisposedMailTest()
		{
			using (var newMail = await _store.NewMailAsync("test-id", "test@example.com", CancellationToken.None))
			using(newMail.BodyStream)
			{
			}
			
			Assert.Empty(Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories));
		}

		[Fact]
		public async Task BasicMailTest()
		{
			string text = @"Header-A: value-a
Header-B: value-b

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			Assert.Equal(text, File.ReadAllText(files[0]));
		}

		[Fact]
		public async Task ExtensionNoHeaderMailTest()
		{
			string text = @"Header-A: value-a
Header-B: value-b

Line 1
Line 2
";

			string expected = @"Header-A: value-a
Header-B: value-b
References: <vaettir.net:original-sender:test-ext@example.com>

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}

		[Fact]
		public async Task ExtensionHeaderBeforeSingleLineMailTest()
		{
			string text = @"Header-A: value-a
Header-B: value-b
References: <other>

Line 1
Line 2
";

			string expected = @"Header-A: value-a
Header-B: value-b
References: <other>
 <vaettir.net:original-sender:test-ext@example.com>

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}

		[Fact]
		public async Task ExtensionHeaderBeforeMultiLineMailTest()
		{
			string text = @"Header-A: value-a
Header-B: value-b
References: <other>
  <x>

Line 1
Line 2
";

			string expected = @"Header-A: value-a
Header-B: value-b
References: <other>
  <x>
 <vaettir.net:original-sender:test-ext@example.com>

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}

		[Fact]
		public async Task ExtensionHeaderAfterSingleLineMailTest()
		{
			string text = @"References: <other>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";

			string expected = @"References: <other>
 <vaettir.net:original-sender:test-ext@example.com>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}

		[Fact]
		public async Task ExtensionHeaderAfterMultiLineMailTest()
		{
			string text = @"References: <other>
  <x>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";

			string expected = @"References: <other>
  <x>
 <vaettir.net:original-sender:test-ext@example.com>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}


		[Fact]
		public async Task ExtensionHeaderSurroundSingleLineMailTest()
		{
			string text = @"Header-Pre: pre-value
References: <other>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";

			string expected = @"Header-Pre: pre-value
References: <other>
 <vaettir.net:original-sender:test-ext@example.com>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}

		[Fact]
		public async Task ExtensionHeaderSurroundMultiLineMailTest()
		{
			string text = @"Header-Pre: pre-value
References: <other>
  <x>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";

			string expected = @"Header-Pre: pre-value
References: <other>
  <x>
 <vaettir.net:original-sender:test-ext@example.com>
Header-A: value-a
Header-B: value-b

Line 1
Line 2
";
			using (var newMail = await _store.NewMailAsync("test-id", "test-ext@example.com", CancellationToken.None))
			{
				using (newMail.BodyStream)
				using (StreamWriter writer = new StreamWriter(newMail.BodyStream))
				{
					await writer.WriteAsync(text);
				}

				await _store.SaveAsync(newMail, CancellationToken.None);
			}
			string[] files = Directory.GetFiles(_storePath, "*", SearchOption.AllDirectories);
			Assert.Single(files);
			var baseName = Path.GetFileName(files[0]);
			Assert.Equal(Path.Combine(_storePath, "example.com", "test", "new"), Path.GetDirectoryName(files[0]));
			Assert.Equal("test-id;2,.mbox", baseName);
			var body = File.ReadAllText(files[0]);
			Assert.Equal(expected, body);
		}

		public void Dispose()
		{
			Directory.Delete(_storePath, true);
		}
	}
}