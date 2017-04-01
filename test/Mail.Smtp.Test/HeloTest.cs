using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Xunit;

namespace Mail.Smtp.Test
{
	public class HelloTest
    {
        [Fact]
        public async Task HeloResponds()
        {
            MockChannel channel = new MockChannel();
            HelloCommand command = new HelloCommand(channel, new SmtpSettings(null, "Testexample.com", null, null, null, null), new MockLogger());
            command.Initialize("Sender.net");
            await command.ExecuteAsync(CancellationToken.None);

            Assert.Equal(1, channel.Entries.Count);
			Assert.True(channel.Entries.All(c => c.Code == ReplyCode.Okay));

			MockChannel.Entry entry = channel.Entries[0];
            Assert.False(entry.More);
            Assert.Contains("Testexample.com", entry.Message);
            Assert.Contains("Sender.net", entry.Message);
        }
    }
}