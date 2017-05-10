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

			Assert.Equal(1, channel.Entries.Count);

			MockSmtpChannel.Entry entry = channel.Entries[0];
			Assert.True(channel.Entries.All(c => c.Code == SmtpReplyCode.Okay));
			Assert.False(entry.More);
			Assert.False(string.IsNullOrEmpty(entry.Message));
		}
	}
}
