using Vaettir.Mail.Server.Smtp;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public static class SmtpTestHelper
	{
		public static void AssertResponse(MockSmtpChannel channel, SmtpReplyCode reply)
		{
			Assert.Equal(1, channel.Entries.Count);
			AssertResponse(channel.Entries[0], reply);
		}

		public static void AssertResponse(MockSmtpChannel.Entry entry, SmtpReplyCode reply)
		{
			Assert.False(entry.More);
			Assert.Equal(reply, entry.Code);
		}
	}
}