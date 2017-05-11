using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class RecipientCommandTest
	{
		[Fact]
		public async Task InvalidAddress()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:!!!!");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.InvalidArguments, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task BadSequenceNoMail()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder();
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:<test@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.BadSequence, entry.Code);
			Assert.False(entry.More);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task RejectForwardPath()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder { PendingMail = mail };
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:<@other:test@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.NameNotAllowed, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task RejectInvalidMailbox()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder { PendingMail = mail };
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:<no-at>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.InvalidArguments, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task RejectExternalDomain()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder { PendingMail = mail };
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:<test@other.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.MailboxUnavailable, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task AcceptLocalDomain()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder { PendingMail = mail };
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:<test@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.Okay, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
			Assert.Equal(1, mail.Recipents.Count);
			Assert.Equal("test@test.vaettir.net", mail.Recipents[0]);
		}

		[Fact]
		public async Task AcceptRelayDomain()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder { PendingMail = mail };
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					relayDomains: new[] {new SmtpRelayDomain("test.vaettir.net", "elsewhere.vaettir.net")})
			);

			command.Initialize("TO:<test@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.Okay, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
			Assert.Equal(1, mail.Recipents.Count);
			Assert.Equal("test@test.vaettir.net", mail.Recipents[0]);
		}

		[Fact]
		public async Task AcceptMultiple()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			mail.Recipents.Add("first@test.vaettir.net");
			var builder = new MockMailBuilder { PendingMail = mail };
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") })
			);

			command.Initialize("TO:<test@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.Okay, entry.Code);
			Assert.False(entry.More);
			Assert.Same(mail, builder.PendingMail);
			SequenceAssert.SameSet(new[] {"first@test.vaettir.net", "test@test.vaettir.net"}, mail.Recipents);
		}
	}
}