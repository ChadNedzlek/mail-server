using System;
using System.Text;
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
		private static (MockMailQueue, MockMailBuilder, MockSmtpChannel, DataCommand) Prepare(
			string content = "",
			AgentSettings settings = null)
		{
			var queue = new MockMailQueue();
			settings = settings ?? TestHelpers.MakeSettings();
			var builder = new MockMailBuilder();
			var channel = new MockSmtpChannel();
			var command = new DataCommand(
				queue,
				settings,
				TestHelpers.GetReader(content),
				new ConnectionInformation("local", "remote"),
				builder,
				channel
			);

			return (queue, builder, channel, command);
		}

		[Fact]
		public async Task AuthenticatedLargeMessageAccepted()
		{
			var expectedBody = "From:box@example.com\r\n\r\nFirst Line\r\nSecond Line\r\n";

			(MockMailQueue queue, MockMailBuilder builder, MockSmtpChannel channel, DataCommand command) = Prepare(
				expectedBody + ".\r\n",
				TestHelpers.MakeSettings(unauthenticatedMessageSizeLimit: 1));

			channel.AuthenticatedUser = new UserData("admin@vaettir.net.test");

			builder.PendingMail = new SmtpMailMessage(new SmtpPath("box@example.com")) {Recipents = {"box@vaettir.net.test"}};
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(2, channel.Entries.Count);
			SmtpTestHelper.AssertResponse(channel.Entries[0], SmtpReplyCode.StartMail);
			SmtpTestHelper.AssertResponse(channel.Entries[1], SmtpReplyCode.Okay);
			Assert.Equal(1, queue.References.Count);
			MockMailReference mailReference = queue.References[0];
			Assert.True(mailReference.IsSaved);
			Assert.Equal("box@example.com", mailReference.Sender);
			SequenceAssert.SameSet(new[] {"box@vaettir.net.test"}, mailReference.Recipients);
			Assert.Throws<ObjectDisposedException>(() => mailReference.BodyStream.WriteByte(1));
			string mailBody = Encoding.UTF8.GetString(mailReference.BackupBodyStream.ToArray());
			Assert.EndsWith(expectedBody, mailBody);
			Assert.StartsWith("Received:", mailBody);
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task NoMailCommand()
		{
			(_, _, MockSmtpChannel channel, DataCommand command) = Prepare();
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
		}

		[Fact]
		public async Task NoRecipients()
		{
			(_, MockMailBuilder builder, MockSmtpChannel channel, DataCommand command) = Prepare();
			builder.PendingMail = new SmtpMailMessage(new SmtpPath("box@example.com"));
			await command.ExecuteAsync(CancellationToken.None);
			SmtpTestHelper.AssertResponse(channel, SmtpReplyCode.BadSequence);
		}

		[Fact]
		public async Task TooLargeMailRejected()
		{
			var expectedBody = "From:box@example.com\r\n\r\nFirst Line\r\nSecond Line\r\n";

			(MockMailQueue queue, MockMailBuilder builder, MockSmtpChannel channel, DataCommand command) = Prepare(
				expectedBody + ".\r\n",
				TestHelpers.MakeSettings(unauthenticatedMessageSizeLimit: 1));

			builder.PendingMail = new SmtpMailMessage(new SmtpPath("box@example.com")) {Recipents = {"box@vaettir.net.test"}};
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(2, channel.Entries.Count);
			SmtpTestHelper.AssertResponse(channel.Entries[0], SmtpReplyCode.StartMail);
			SmtpTestHelper.AssertResponse(channel.Entries[1], SmtpReplyCode.ExceededQuota);
			Assert.Equal(1, queue.References.Count);
			MockMailReference mailReference = queue.References[0];
			Assert.False(mailReference.IsSaved);
			Assert.Throws<ObjectDisposedException>(() => mailReference.BodyStream.WriteByte(1));
			Assert.Null(builder.PendingMail);
		}

		[Fact]
		public async Task ValidMailSaved()
		{
			var expectedBody = "From:box@example.com\r\n\r\nFirst Line\r\nSecond Line\r\n";
			(MockMailQueue queue, MockMailBuilder builder, MockSmtpChannel channel, DataCommand command) =
				Prepare(expectedBody + ".\r\n");
			builder.PendingMail = new SmtpMailMessage(new SmtpPath("box@example.com")) {Recipents = {"box@vaettir.net.test"}};
			await command.ExecuteAsync(CancellationToken.None);
			Assert.Equal(2, channel.Entries.Count);
			SmtpTestHelper.AssertResponse(channel.Entries[0], SmtpReplyCode.StartMail);
			SmtpTestHelper.AssertResponse(channel.Entries[1], SmtpReplyCode.Okay);
			Assert.Equal(1, queue.References.Count);
			MockMailReference mailReference = queue.References[0];
			Assert.True(mailReference.IsSaved);
			Assert.Equal("box@example.com", mailReference.Sender);
			SequenceAssert.SameSet(new[] {"box@vaettir.net.test"}, mailReference.Recipients);
			Assert.Throws<ObjectDisposedException>(() => mailReference.BodyStream.WriteByte(1));
			string mailBody = Encoding.UTF8.GetString(mailReference.BackupBodyStream.ToArray());
			Assert.EndsWith(expectedBody, mailBody);
			Assert.StartsWith("Received:", mailBody);
			Assert.Null(builder.PendingMail);
		}
	}
}
