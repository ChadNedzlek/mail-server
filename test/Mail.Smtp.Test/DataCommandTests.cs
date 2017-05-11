using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Xunit;

namespace Vaettir.Mail.Smtp.Test
{
	public class DataCommandTests
	{
		[Fact]
		public async Task NoMailCommand()
		{
			var (_, _, _, channel, command) = Prepare();
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
		}

		[Fact]
		public async Task NoRecipients()
		{
			var (_, _, builder, channel, command) = Prepare();
			builder.PendingMail = new SmtpMailMessage(new SmtpPath("box@example.com"));
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
		}

		private (MockMailQueue, AgentSettings, MockMailBuilder, MockSmtpChannel, DataCommand) Prepare(params string [] dataLines)
		{
			var queue = new MockMailQueue();
			AgentSettings settings = TestHelpers.MakeSettings();
			var builder = new MockMailBuilder();
			var channel = new MockSmtpChannel();
			var command = new DataCommand(
				queue,
				settings,
				TestHelpers.GetReader(dataLines),
				new ConnectionInformation("local", "remote"),
				builder,
				channel
			);

			return (queue, settings, builder, channel, command);
		}
	}
}