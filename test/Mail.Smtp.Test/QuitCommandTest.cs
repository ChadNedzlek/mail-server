using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class QuitCommandTest
	{
		[Fact]
		public async Task QuitClosesConnectionAfterReply()
		{
			var channel = new MockSmtpChannel();
			var command = new QuitCommand(channel);
			command.Initialize("");
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Closing);
			Assert.True(channel.IsClosed);
		}
	}
}