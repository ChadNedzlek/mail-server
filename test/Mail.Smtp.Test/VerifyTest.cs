using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class VerifyTest
	{
		[Fact]
		public async Task NotSupported()
		{
			MockSmtpChannel channel = new MockSmtpChannel();
			var command = new VerifyCommand(channel);
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.CannotVerify);
		}
	}
}