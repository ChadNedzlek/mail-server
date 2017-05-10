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
			Assert.Equal(1, channel.Entries.Count);
			MockSmtpChannel.Entry entry = channel.Entries[0];
			Assert.Equal(SmtpReplyCode.CannotVerify, entry.Code);
			Assert.False(entry.More);
		}
	}
}