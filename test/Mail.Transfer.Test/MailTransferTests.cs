using System;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Mail.Transfer;
using Vaettir.Utility;
using Xunit;

namespace Mail.Transfer.Test
{
	public class MailTransferTests : IDisposable
	{
		private readonly MockMailTransferQueue _queue;
		private readonly MockVolatile<SmtpSettings> _settings;
		private readonly MockDnsResolve _dns;
		private readonly MockMailSendFailureManager _failures;
		private readonly MockTcpConnectionProvider _tcp;
		private readonly MailTransfer _transfer;

		public MailTransferTests()
		{
			_queue = new MockMailTransferQueue();
			_settings = new MockVolatile<SmtpSettings>(new SmtpSettings());
			_dns = new MockDnsResolve();
			_failures = new MockMailSendFailureManager();
			_tcp = new MockTcpConnectionProvider();
			_transfer = new MailTransfer(
				_queue,
				_settings,
				new MockLogger(),
				_dns,
				_failures,
				_tcp);
		}

		[Fact]
		public void FreshMailIsReadyToSend()
		{
			Assert.True(_transfer.IsReadyToSend(
				new MockMailReference(
					"mock-1",
					"test@test.example.com",
					new[] {"test@external.example.com"}.ToImmutableList(),
					true,
					_queue)));
		}

	    [Fact]
	    public void RepeatMailIsNotReadyYet()
	    {
	        _failures.AddFailure("mock-1", DateTimeOffset.UtcNow, 5);
			Assert.False(_transfer.IsReadyToSend(
				new MockMailReference(
					"mock-1",
					"test@test.example.com",
					new[] { "test@external.example.com" }.ToImmutableList(),
					true,
					_queue)));
		}

		[Fact]
		public void NewMailIsReadyToSend()
		{
			Assert.True(_transfer.IsReadyToSend(
				new MockMailReference(
					"mock-1",
					"test@test.example.com",
					new[] { "test@external.example.com" }.ToImmutableList(),
					true,
					_queue)));
		}

		[Fact]
		public void BadMailIsNotReadyToSend()
		{
			_failures.AddFailure("mock-1", DateTimeOffset.UtcNow, 5);
			Assert.False(_transfer.IsReadyToSend(
				new MockMailReference(
					"mock-1",
					"test@test.example.com",
					new[] { "test@external.example.com" }.ToImmutableList(),
					true,
					_queue)));
		}

		[Fact]
		public async Task ReadSingleResponse()
		{
			var responseMessages = @"250 example.com greets test.example.com
";
			using (MemoryStream outStream = new MemoryStream())
			using (MemoryStream inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (StreamReader reader = new StreamReader(inStream))
			using (StreamWriter writer = new StreamWriter(outStream))
			{
				var response = await _transfer.ExecuteRemoteCommandAsync(writer, reader, "HELO test.example.com");
				Assert.Equal("HELO test.example.com" + Environment.NewLine, Encoding.ASCII.GetString(outStream.ToArray()));
				Assert.Equal(ReplyCode.Okay, response.Code);
				Assert.Equal(new[] { "example.com greets test.example.com" }, response.Lines);
			}
		}

		[Fact]
		public async Task ReadMultiResponse()
		{
			var responseMessages = @"250-STARTTLS
250 example.com greets test.example.com
";
			using (MemoryStream outStream = new MemoryStream())
			using (MemoryStream inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (StreamReader reader = new StreamReader(inStream))
			using (StreamWriter writer = new StreamWriter(outStream))
			{
				var response = await _transfer.ExecuteRemoteCommandAsync(writer, reader, "EHLO test.example.com");
				Assert.Equal("EHLO test.example.com" + Environment.NewLine, Encoding.ASCII.GetString(outStream.ToArray()));
				Assert.Equal(ReplyCode.Okay, response.Code);
				Assert.Equal(new[] { "STARTTLS", "example.com greets test.example.com" }, response.Lines);
			}
		}

		[Fact]
		public void ShouldAttemptRetryAfterSingleFailure()
		{
			Assert.True(_transfer.ShouldAttemptRedeliveryAfterFailure(
				new MockMailReference(
					"mock-1",
					"test@test.example.com",
					new[] { "test@external.example.com" }.ToImmutableList(),
					true,
					_queue)));
			SmtpFailureData failure = _failures.GetFailure("mock-1", false);
			Assert.NotNull(failure);
			Assert.Equal(1, failure.Retries);
			Assert.InRange(failure.FirstFailure, DateTimeOffset.UtcNow - TimeSpan.FromSeconds(1), DateTime.UtcNow);
		}

		[Fact]
		public void ShouldNotAttemptRetryAfterManyFailure()
		{
			_failures.AddFailure("mock-1", DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)), 100);
			Assert.False(_transfer.ShouldAttemptRedeliveryAfterFailure(
				new MockMailReference(
					"mock-1",
					"test@test.example.com",
					new[] { "test@external.example.com" }.ToImmutableList(),
					true,
					_queue)));
		}

		[Fact]
		public async Task BasicFailedConversation()
		{
			var responseMessages = @"554 No clue
554 No clue
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] { "test@external.example.com" }.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);

			using (MemoryStream outStream = new MemoryStream())
			using (MemoryStream inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (StreamReader reader = new StreamReader(inStream))
			using (StreamWriter writer = new StreamWriter(outStream))
			{
				Assert.False(await _transfer.TrySendSingleMailAsync(mockMailReference, writer, reader, CancellationToken.None));
			}
			Assert.Equal(1, _queue.References.Count);
			Assert.Equal(0, _queue.DeletedReferences.Count);
		}

		[Fact]
		public async Task SendSingle()
		{
			var responseMessages = @"250 Ok (MAIL)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] { "test@external.example.com" }.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);

			using (MemoryStream outStream = new MemoryStream())
			using (MemoryStream inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (StreamReader reader = new StreamReader(inStream))
			using (StreamWriter writer = new StreamWriter(outStream))
			{
				Assert.True(await _transfer.TrySendSingleMailAsync(mockMailReference, writer, reader, CancellationToken.None));
			}
			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);
		}

		[Fact]
		public async Task ProcessAll_NoEhloTest()
		{
			var responseMessages = @"554 No clue (EHLO)
220 example.com greets test.example.com (HELO)
250 Ok (MAIL)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] { "test@external.example.com" }.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "mx.external.example.com", 10);
			_dns.AddIp("mx.external.example.com", IPAddress.Parse("10.20.30.40"));

			var (keep, give) = PairedStream.Create();

			var responseBytes = Encoding.ASCII.GetBytes(responseMessages);
			await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

			Assert.True(await _transfer.TrySendMailsToStream(
				"external.example.com",
				new[] { mockMailReference },
				give,
				CancellationToken.None));

			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);

			keep.Dispose();
			give.Dispose();
		}

		[Fact]
		public async Task ProcessAll_EhloNoStartTlsTest()
		{
			var responseMessages = @"220 example.com greets test.example.com (HELO)
250 Ok (MAIL)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] { "test@external.example.com" }.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "mx.external.example.com", 10);
			_dns.AddIp("mx.external.example.com", IPAddress.Parse("10.20.30.40"));

			var (keep, give) = PairedStream.Create();

			var responseBytes = Encoding.ASCII.GetBytes(responseMessages);
			await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

			Assert.True(await _transfer.TrySendMailsToStream(
				"external.example.com",
				new[] { mockMailReference },
				give,
				CancellationToken.None));

			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);

			keep.Dispose();
			give.Dispose();
		}

		[Fact]
		public async Task ProcessAll_EhloStartTlsTest()
		{
			var responseMessagesPreEncrypt = @"220-STARTTLS
200 example.com greets test.example.com (HELO)
220 Ok, begin encryption
";
			var responseMessagesPostEncrypt = @"250 Ok (MAIL)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] { "test@external.example.com" }.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "mx.external.example.com", 10);
			_dns.AddIp("mx.external.example.com", IPAddress.Parse("10.20.30.40"));

			var (keep, give) = PairedStream.Create();

			var responseBytes = Encoding.ASCII.GetBytes(responseMessagesPreEncrypt);
			await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

			SslStream encrypted = new SslStream(keep);

			Task asServerAsync = encrypted.AuthenticateAsServerAsync(TestHelpers.GetSelfSigned());
			Task<bool> trySendMailsToStream = _transfer.TrySendMailsToStream(
				"external.example.com",
				new[] { mockMailReference },
				give,
				CancellationToken.None);

			var completed = await Task.WhenAny(asServerAsync, trySendMailsToStream);

			Assert.Same(completed, asServerAsync);

			responseBytes = Encoding.ASCII.GetBytes(responseMessagesPostEncrypt);
			await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

			Assert.True(await trySendMailsToStream);

			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);

			keep.Dispose();
			give.Dispose();
		}

		public void Dispose()
		{
			_settings?.Dispose();
			_tcp?.Dispose();
		}
	}
}
