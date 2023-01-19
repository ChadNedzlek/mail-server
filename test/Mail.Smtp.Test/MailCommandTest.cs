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
		public async Task FromLocalDomainAnonymousRejected()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder();
			var user = new MockUserStore(false);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<bad@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task FromWrongMailboxRejected()
		{
			var channel = new MockSmtpChannel {AuthenticatedUser = new UserData("good@vaettir.net.test")};
			var builder = new MockMailBuilder();
			var user = new MockUserStore(false);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<bad@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.MailboxUnavailable);
			Assert.Null(builder.PendingMail);
		}

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
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:!!!!");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
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
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<good@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
			Assert.Same(message, builder.PendingMail);
		}

		[Fact]
		public async Task VailMailAccepted()
		{
			var channel = new MockSmtpChannel {AuthenticatedUser = new UserData("good@vaettir.net.test")};
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<good@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.NotNull(builder.PendingMail);
			Assert.Equal("good@vaettir.net.test", builder.PendingMail.FromPath.Mailbox);
		}

		[Fact]
		public async Task VailMailAcceptedWithReturnPathRejected()
		{
			var channel = new MockSmtpChannel {AuthenticatedUser = new UserData("good@vaettir.net.test")};
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<@other:good@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task VailMailWithParametersAccepted()
		{
			var channel = new MockSmtpChannel {AuthenticatedUser = new UserData("good@vaettir.net.test")};
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<good@vaettir.net.test> BODY=7BIT");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.NotNull(builder.PendingMail);
			Assert.Equal("good@vaettir.net.test", builder.PendingMail.FromPath.Mailbox);
		}

		[Fact]
		public async Task VailMailWithUnknownRejected()
		{
			var channel = new MockSmtpChannel {AuthenticatedUser = new UserData("good@vaettir.net.test")};
			var builder = new MockMailBuilder();
			var user = new MockUserStore(true);
			var command = new MailCommand(
				channel,
				builder,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<good@vaettir.net.test> EVIL=FAIL");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.ParameterNotImplemented);
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
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")}),
				user);

			command.Initialize("FROM:<>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.NotNull(builder.PendingMail);
			Assert.Equal("", builder.PendingMail.FromPath.Mailbox);
		}
	}
}
