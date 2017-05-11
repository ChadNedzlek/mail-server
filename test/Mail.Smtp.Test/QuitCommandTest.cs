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
			Assert.Equal(1, channel.Entries.Count);
			Assert.Equal(SmtpReplyCode.Closing, channel.Entries[0].Code);
			Assert.False(channel.Entries[0].More);
			Assert.True(channel.IsClosed);
		}
	}
}