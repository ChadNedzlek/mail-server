using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class NoopTest
	{
		[Fact]
		public async Task NoopSendsMessage()
		{
			var channel = new MockSmtpChannel();
			var noop = new NoopCommand(channel);
			noop.Initialize("");
			await noop.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.Okay);
		}
	}
}
