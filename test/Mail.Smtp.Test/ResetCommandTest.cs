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
			Assert.Equal(1, channel.Entries.Count);
			Assert.Equal(SmtpReplyCode.Okay, channel.Entries[0].Code);
			Assert.False(channel.Entries[0].More);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task ResetAfterMailCommand()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder { PendingMail = new SmtpMailMessage(new SmtpPath("someone@example.com")) };
			var command = new ResetCommand(channel, builder);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			Assert.Equal(SmtpReplyCode.Okay, channel.Entries[0].Code);
			Assert.False(channel.Entries[0].More);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task ResetAfterRcptCommand()
		{
			var channel = new MockSmtpChannel();
			var builder = new MockMailBuilder { PendingMail = new SmtpMailMessage(new SmtpPath("someone@example.com")) };
			builder.PendingMail.Recipents.Add("in@test.vaettir.net");
			var command = new ResetCommand(channel, builder);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(1, channel.Entries.Count);
			Assert.Equal(SmtpReplyCode.Okay, channel.Entries[0].Code);
			Assert.False(channel.Entries[0].More);
			Assert.Null(builder.PendingMail);
		}
	}
}