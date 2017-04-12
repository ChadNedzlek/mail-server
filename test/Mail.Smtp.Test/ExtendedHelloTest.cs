using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Utility.Test;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
    public class ExtendedHelloTest
	{
		[Fact]
		public async Task Unsecured_NoCertificate_ExtendedHelloResponds()
		{
			var channel = new MockChannel();
			var conn = new MockConnectionSecurity();

			var command = new ExtendedHelloCommand(
				TestHelpers.GetAuths(),
				conn,
				channel,
				new SmtpSettings(domainName: "test.vaettir.net"),
				new MockLogger());
			command.Initialize("Sender.net");

			await command.ExecuteAsync(CancellationToken.None);

			Assert.True(channel.Entries.All(c => c.Code == ReplyCode.Okay));
			Assert.True(channel.Entries.Take(channel.Entries.Count - 1).All(e => e.More));
			Assert.False(channel.Entries.Last().More);

			Assert.DoesNotContain(channel.Entries, e => e.Message == "STARTTLS");
			Assert.DoesNotContain(channel.Entries, e => e.Message == "AUTH ENC");
			Assert.Contains(channel.Entries, e => e.Message == "AUTH PLN");

			var signoff = channel.Entries.First();
			Assert.Contains("test.vaettir.net", signoff.Message);
			Assert.Contains("Sender.net", signoff.Message);
		}

		[Fact]
		public async Task Unsecured_Certificate_ExtendedHelloResponds()
		{
			var channel = new MockChannel();
			var conn = new MockConnectionSecurity();
			conn.Certificate = TestHelpers.GetSelfSigned();

			var command = new ExtendedHelloCommand(
				TestHelpers.GetAuths(),
				conn,
				channel,
				new SmtpSettings(domainName: "test.vaettir.net"),
				new MockLogger());
			command.Initialize("Sender.net");

			await command.ExecuteAsync(CancellationToken.None);

			Assert.True(channel.Entries.All(c => c.Code == ReplyCode.Okay));
			Assert.True(channel.Entries.Take(channel.Entries.Count - 1).All(e => e.More));
			Assert.False(channel.Entries.Last().More);

			Assert.Contains(channel.Entries, e => e.Message == "STARTTLS");
		    var authReplies = channel.Entries.Where(e => e.Message.StartsWith("AUTH")).ToList();
		    Assert.Equal(1, authReplies.Count);
		    var authParts = authReplies[0].Message.Split(' ').Skip(1).ToList();
			SequenceAssert.SameSet(new[] { "PLN" }, authParts);

			var signoff = channel.Entries.First();
			Assert.Contains("test.vaettir.net", signoff.Message);
			Assert.Contains("Sender.net", signoff.Message);
		}

		[Fact]
		public async Task Secured_ExtendedHelloResponds()
		{
			var channel = new MockChannel();
			var conn = new MockConnectionSecurity()
			{
				Certificate = TestHelpers.GetSelfSigned(),
				IsEncrypted = true
			};

			var command = new ExtendedHelloCommand(
				TestHelpers.GetAuths(),
				conn,
				channel,
				new SmtpSettings(domainName: "test.vaettir.net"),
				new MockLogger());
			command.Initialize("Sender.net");

			await command.ExecuteAsync(CancellationToken.None);

			Assert.True(channel.Entries.All(c => c.Code == ReplyCode.Okay));
			Assert.True(channel.Entries.Take(channel.Entries.Count - 1).All(e => e.More));
			Assert.False(channel.Entries.Last().More);

			Assert.DoesNotContain(channel.Entries, e => e.Message == "STARTTLS");
			var authReplies = channel.Entries.Where(e => e.Message.StartsWith("AUTH")).ToList();
			Assert.Equal(1, authReplies.Count);
			var authParts = authReplies[0].Message.Split(' ').Skip(1).ToList();
			SequenceAssert.SameSet(new[] { "PLN", "ENC" }, authParts);

			var signoff = channel.Entries.First();
			Assert.Contains("test.vaettir.net", signoff.Message);
			Assert.Contains("Sender.net", signoff.Message);
		}
	}

    public class MockConnectionSecurity : IConnectionSecurity
    {
        public X509Certificate2 Certificate { get; set; }
        public bool IsEncrypted { get; set; }
    }
}