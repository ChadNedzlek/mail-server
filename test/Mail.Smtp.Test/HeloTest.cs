using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class HelloTest
	{
		[Fact]
		public async Task HeloResponds()
		{
			var channel = new MockChannel();
			var command = new HelloCommand(channel, TestHelpers.MakeSettings(domainName: "Testexample.com"), new MockLogger());
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
