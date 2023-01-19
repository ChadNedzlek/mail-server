using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class ResetCommandTest
	{
		[Fact]
		public async Task NullStateReset()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder();
			var command = new ResetCommand(channel, builder);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task ResetAfterMailCommand()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder {PendingMail = new SmtpMailMessage(new SmtpPath("someone@example.com"))};
			var command = new ResetCommand(channel, builder);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task ResetAfterRcptCommand()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder {PendingMail = new SmtpMailMessage(new SmtpPath("someone@example.com"))};
			builder.PendingMail.Recipents.Add("in@vaettir.net.test");
			var command = new ResetCommand(channel, builder);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
			Assert.Null(builder.PendingMail);
		}
	}
}
