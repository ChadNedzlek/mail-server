using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class RecipientCommandTest
	{
		[Fact]
		public async Task AcceptLocalDomain()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:<test@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.Same(mail, builder.PendingMail);
			Assert.Single(mail.Recipents);
			Assert.Equal("test@vaettir.net.test", mail.Recipents[0]);
		}

		[Fact]
		public async Task AcceptMultiple()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			mail.Recipents.Add("first@vaettir.net.test");
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:<test@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.Same(mail, builder.PendingMail);
			SequenceAssert.SameSet(new[] {"first@vaettir.net.test", "test@vaettir.net.test"}, mail.Recipents);
		}

		[Fact]
		public async Task AcceptRelayDomain()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					relayDomains: new[] {new SmtpRelayDomain("vaettir.net.test", "elsewhere.vaettir.net")})
			);

			command.Initialize("TO:<test@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.Same(mail, builder.PendingMail);
			Assert.Single(mail.Recipents);
			Assert.Equal("test@vaettir.net.test", mail.Recipents[0]);
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
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:<test@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
			Assert.Null(builder.PendingMail);
		}

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
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:!!!!");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task RejectExternalDomain()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:<test@other.vaettir.net>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.MailboxUnavailable);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task RejectForwardPath()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:<@other:test@vaettir.net.test>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.NameNotAllowed);
			Assert.Same(mail, builder.PendingMail);
		}

		[Fact]
		public async Task RejectInvalidMailbox()
		{
			var channel = new MockSmtpChannel();
			var mail = new SmtpMailMessage(new SmtpPath("someone@example.com"));
			var builder = new MockMailBuilder {PendingMail = mail};
			var command = new RecipientCommand(
				builder,
				channel,
				TestHelpers.MakeSettings(
					"vaettir.net.test",
					new[] {new SmtpAcceptDomain("vaettir.net.test")})
			);

			command.Initialize("TO:<no-at>");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Same(mail, builder.PendingMail);
		}
	}
}
