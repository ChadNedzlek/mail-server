using Vaettir.Mail.Server.Smtp;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public static class SmtpTestHelper
	{
		public static void AssertResponse(MockSmtpChannel channel, SmtpReplyCode reply)
		{
			Assert.Equal(1, channel.Entries.Count);
			Assert.False(channel.Entries[0].More);
			Assert.Equal(reply, channel.Entries[0].Code);
		}
	}
}