using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class SmtpAuthenctiationTransportTest
	{
		[Fact]
		public async Task Roundtrip()
		{
			var channel = new MockSmtpChannel();
			SmtpAuthenticationTransport trans = new SmtpAuthenticationTransport(
				channel,
				TestHelpers.GetReader("AQID\r\n")
				);

			await trans.SendAuthenticationFragmentAsync(new byte[] {4, 5, 6}, CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.AuthenticationFragment);
			Assert.Equal("BAUG", channel.Entries[0].Message);

			var bytes = await trans.ReadAuthenticationFragmentAsync(CancellationToken.None);

			Assert.Equal(new byte[] {1, 2, 3}, bytes);
		}
	}
}