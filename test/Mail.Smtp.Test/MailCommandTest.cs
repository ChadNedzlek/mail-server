using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class MailCommandTest
	{
		[Fact]
		public async Task InvalidDomainRejected()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder();
			var user = new MockUserStore(false);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:!!!!");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.InvalidArguments, entry.Code);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task FromLocalDomainAnonymousRejected()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder();
			var user = new MockUserStore(false);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:<bad@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.InvalidArguments, entry.Code);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task FromWrongMailboxRejected()
		{
			var channel = new MockSmtpChannel { AuthenticatedUser = new UserData("good@test.vaettir.net") };
			var builder = new MockMailBuilder();
			var user = new MockUserStore(false);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:<bad@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.MailboxUnavailable, entry.Code);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task MailInProgressRejected()
		{
			var channel = new MockSmtpChannel();
			var message = new SmtpMailMessage(new SmtpPath(null));
			var builder = new MockMailBuilder
			{
				PendingMail = message
			};
			var user = new MockUserStore(false);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:<good@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.BadSequence, entry.Code);
			Assert.Same(message, builder.PendingMail);
		}

		[Fact]
		public async Task VailMailAccepted()
		{
			var channel = new MockSmtpChannel { AuthenticatedUser = new UserData("good@test.vaettir.net") };
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:<good@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.Okay, entry.Code);
			Assert.NotNull(builder.PendingMail);
			Assert.Equal("good@test.vaettir.net", builder.PendingMail.FromPath.Mailbox);
		}

		[Fact]
		public async Task VailMailAcceptedWithReturnPathRejected()
		{
			var channel = new MockSmtpChannel { AuthenticatedUser = new UserData("good@test.vaettir.net") };
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:<@other:good@test.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.InvalidArguments, entry.Code);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task VailToInternal()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					domainName: "test.vaettir.net",
					localDomains: new[] { new SmtpAcceptDomain("test.vaettir.net") }),
				user);

			command.Initialize("FROM:<>");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			var entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.Okay, entry.Code);
			Assert.NotNull(builder.PendingMail);
			Assert.Equal("", builder.PendingMail.FromPath.Mailbox);
		}
	}
}