using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class AuthenticateCommandTest
	{
		[Fact]
		public async Task AlreadyAuthed()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Null)}
			};

			var user = new UserData("someone@example.com");
			var channel = new MockSmtpChannel {AuthenticatedUser = user};
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("PLAIN");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
			Assert.Same(user, channel.AuthenticatedUser);
		}

		[Fact]
		public async Task BadArguments()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Throw)}
			};

			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("PLAIN");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Null(channel.AuthenticatedUser);
		}

		[Fact]
		public async Task DuringMailSession()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Null)}
			};

			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder {PendingMail = new SmtpMailMessage(new SmtpPath("me@test.vaettir.net"))};
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("PLAIN");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
			Assert.Null(channel.AuthenticatedUser);
		}

		[Fact]
		public async Task MissingMechanism()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Null)}
			};

			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("MISSING");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Null(channel.AuthenticatedUser);
		}

		[Fact]
		public async Task PlainReturnsNull()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Null)}
			};

			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("PLAIN");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.AuthencticationCredentialsInvalid);
			Assert.Null(channel.AuthenticatedUser);
		}

		[Fact]
		public async Task SuccessfulAuth()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Return)}
			};

			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("PLAIN");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.AuthenticationComplete);
			Assert.NotNull(channel.AuthenticatedUser);
			Assert.Equal(MockPlainTextAuth.UserMailbox, channel.AuthenticatedUser.Mailbox);
		}

		[Fact]
		public async Task TooFewArguments()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Null)}
			};
			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Null(channel.AuthenticatedUser);
		}

		[Fact]
		public async Task TooManyArguments()
		{
			var auth = new MockIndex<string, IAuthenticationSession>
			{
				{"PLAIN", new MockPlainTextAuth(MockPlainTextAuth.Action.Null)}
			};
			var channel = new MockSmtpChannel();
			var mockMailBuilder = new MockMailBuilder();
			var command = new AuthenticateCommand(auth, channel, mockMailBuilder);
			command.Initialize("MECH INITIAL");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.InvalidArguments);
			Assert.Null(channel.AuthenticatedUser);
		}
	}
}
