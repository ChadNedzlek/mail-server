using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Utility;
using Xunit;

namespace Vaettir.Mail.Transfer.Test
{
	public class MailTransferTests : IDisposable
	{
		public MailTransferTests()
		{
			_queue = new MockMailTransferQueue();
			_settings = new MockVolatile<AgentSettings>(
				TestHelpers.MakeSettings(
					relayDomains: new[] {new SmtpRelayDomain("relay.example.com", "relaytarget.example.com", 99)}
				));
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

		public void Dispose()
		{
			_settings?.Dispose();
			_tcp?.Dispose();
		}

		private readonly MockMailTransferQueue _queue;
		private readonly MockVolatile<AgentSettings> _settings;
		private readonly MockDnsResolve _dns;
		private readonly MockMailSendFailureManager _failures;
		private readonly MockTcpConnectionProvider _tcp;
		private readonly MailTransfer _transfer;

		private Task WriteToAsync(Stream stream, string message)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(message);
			return stream.WriteAsync(bytes, 0, bytes.Length);
		}

		private async Task<MockTcpConnectionProvider.MockTcpClient> GetClientFor(IPAddress ipAddress)
		{
			MockTcpConnectionProvider.MockTcpClient client = null;
			while (client?.HalfStream == null)
			{
				client = _tcp.Created.FirstOrDefault(t => Equals(t.IpAddress, ipAddress));
				if (client?.HalfStream == null)
				{
					await Task.Delay(100);
				}
			}

			return client;
		}

		[SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
		private static async Task AssertCommandRecieved(TextReader reader, string command)
		{
			Assert.Equal(command, (await reader.ReadLineAsync()).Split(' ')[0]);
		}

		[Fact]
		public void BadMailIsNotReadyToSend()
		{
			_failures.AddFailure("mock-1", DateTimeOffset.UtcNow, 5);
			Assert.False(
				_transfer.IsReadyToSend(
					new MockMailReference(
						"mock-1",
						"test@test.example.com",
						new[] {"test@external.example.com"}.ToImmutableList(),
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
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);

			using (var outStream = new MemoryStream())
			using (var inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (var reader = new StreamReader(inStream))
			using (var writer = new StreamWriter(outStream))
			{
				Assert.False(await _transfer.TrySendSingleMailAsync(mockMailReference, writer, reader, CancellationToken.None));
			}

			Assert.Equal(1, _queue.References.Count);
			Assert.Equal(0, _queue.DeletedReferences.Count);
		}

		[Fact]
		public void FreshMailIsReadyToSend()
		{
			Assert.True(
				_transfer.IsReadyToSend(
					new MockMailReference(
						"mock-1",
						"test@test.example.com",
						new[] {"test@external.example.com"}.ToImmutableList(),
						true,
						_queue)));
		}

		[Fact]
		public async Task LookupWithFallback()
		{
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@example.com",
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("example.com", "second.example.com", 20);
			_dns.AddMx("example.com", "first.example.com", 10);
			_dns.AddMx("example.com", "third.example.com", 30);

			_dns.AddIp("first.example.com", IPAddress.Parse("10.0.0.1"));
			_dns.AddIp("second.example.com", IPAddress.Parse("10.0.0.2"));
			_dns.AddIp("third.example.com", IPAddress.Parse("10.0.0.3"));

			Task executeTask = Task.Run(
				() => _transfer.SendMailsToDomain("example.com", new[] {mockMailReference}, CancellationToken.None));

			MockTcpConnectionProvider.MockTcpClient client = await GetClientFor(IPAddress.Parse("10.0.0.1"));
			await WriteToAsync(
				client.HalfStream,
				@"554 Failed
554 Failed
");
			client = await GetClientFor(IPAddress.Parse("10.0.0.2"));
			await WriteToAsync(
				client.HalfStream,
				@"554 Failed
554 Failed
");
			client = await GetClientFor(IPAddress.Parse("10.0.0.3"));
			await WriteToAsync(
				client.HalfStream,
				@"220 example.com greets test.example.com (HELO)
250 Ok (MAIL)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
");

			await executeTask;
			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);
		}

		[Fact]
		public async Task LookupWithFallback_FailedAll()
		{
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@example.com",
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("example.com", "first.example.com", 10);
			_dns.AddIp("first.example.com", IPAddress.Parse("10.0.0.1"));

			Task executeTask = Task.Run(
				() => _transfer.SendMailsToDomain("example.com", new[] {mockMailReference}, CancellationToken.None));

			MockTcpConnectionProvider.MockTcpClient client = await GetClientFor(IPAddress.Parse("10.0.0.1"));
			await WriteToAsync(
				client.HalfStream,
				@"554 Failed
554 Failed
");
			await executeTask;
			Assert.Equal(1, _queue.References.Count);
			Assert.Equal(0, _queue.DeletedReferences.Count);

			SmtpFailureData failure = _failures.GetFailure("mock-1", false);
			Assert.NotNull(failure);
			Assert.Equal(1, failure.Retries);
			Assert.InRange(failure.FirstFailure, DateTimeOffset.UtcNow - TimeSpan.FromSeconds(1), DateTime.UtcNow);
		}

		[Fact]
		public void NewMailIsReadyToSend()
		{
			Assert.True(
				_transfer.IsReadyToSend(
					new MockMailReference(
						"mock-1",
						"test@test.example.com",
						new[] {"test@external.example.com"}.ToImmutableList(),
						true,
						_queue)));
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
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "mx.external.example.com", 10);
			_dns.AddIp("mx.external.example.com", IPAddress.Parse("10.20.30.40"));

			(Stream keep, Stream give) = PairedStream.Create();
			using (var reader = new StreamReader(keep))
			{
				byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessages);
				await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

				Assert.Empty(
					await _transfer.TrySendMailsToStream(
						"external.example.com",
						new[] {mockMailReference},
						new UnclosableStream(give),
						CancellationToken.None));

				Assert.Equal(0, _queue.References.Count);
				Assert.Equal(1, _queue.DeletedReferences.Count);

				await AssertCommandRecieved(reader, "EHLO");
				await AssertCommandRecieved(reader, "MAIL");
				await AssertCommandRecieved(reader, "RCPT");
				await AssertCommandRecieved(reader, "DATA");

				give.Dispose();
			}
		}

		[Fact]
		public async Task ProcessAll_EhloStartTlsTest()
		{
			var responseMessagesPreEncrypt = @"220-STARTTLS
220 example.com greets test.example.com (EHLO)
250 Ok, begin encryption (STARTTLS)
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
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "test.example.com", 10);
			_dns.AddIp("test.example.com", IPAddress.Parse("10.20.30.40"));

			(Stream keep, Stream give) = PairedStream.Create();

			using (var recieveStream = new RedirectableStream(keep))
			using (var reader = new StreamReader(recieveStream))
			{
				Task<IReadOnlyList<IMailReference>> sendMailsTask = Task.Run(
					() => _transfer.TrySendMailsToStream(
						"external.example.com",
						new[] {mockMailReference},
						// ReSharper disable once AccessToDisposedClosure
						new UnclosableStream(give),
						CancellationToken.None));

				_transfer.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true;

				byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessagesPreEncrypt);
				await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

				await AssertCommandRecieved(reader, "EHLO");
				await AssertCommandRecieved(reader, "STARTTLS");

				var encrypted = new SslStream(keep);

				await encrypted.AuthenticateAsServerAsync(TestHelpers.GetSelfSigned());
				recieveStream.ChangeSteam(encrypted);

				responseBytes = Encoding.ASCII.GetBytes(responseMessagesPostEncrypt);
				await encrypted.WriteAsync(responseBytes, 0, responseBytes.Length);

				Assert.Empty(await sendMailsTask);

				Assert.Equal(0, _queue.References.Count);
				Assert.Equal(1, _queue.DeletedReferences.Count);

				encrypted.Dispose();
				keep.Dispose();
				give.Dispose();
			}
		}

		[Fact]
		public async Task ProcessAll_EhloStartTlsTest_NotTrusted()
		{
			var responseMessagesPreEncrypt = @"220-STARTTLS
220 example.com greets test.example.com (EHLO)
250 Ok, begin encryption (STARTTLS)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "test.example.com", 10);
			_dns.AddIp("test.example.com", IPAddress.Parse("10.20.30.40"));

			(Stream keep, Stream give) = PairedStream.Create();

			using (var recieveStream = new RedirectableStream(keep))
			using (var reader = new StreamReader(recieveStream))
			{
				Task<IReadOnlyList<IMailReference>> sendMailsTask = Task.Run(
					() => _transfer.TrySendMailsToStream(
						"external.example.com",
						new[] {mockMailReference},
						// ReSharper disable once AccessToDisposedClosure
						new UnclosableStream(give),
						CancellationToken.None));

				_transfer.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => false;

				byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessagesPreEncrypt);
				await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

				await AssertCommandRecieved(reader, "EHLO");
				await AssertCommandRecieved(reader, "STARTTLS");

				var encrypted = new SslStream(keep);

				await encrypted.AuthenticateAsServerAsync(TestHelpers.GetSelfSigned());
				Assert.NotEmpty(await sendMailsTask);

				Assert.Equal(1, _queue.References.Count);
				Assert.Equal(0, _queue.DeletedReferences.Count);

				encrypted.Dispose();
				keep.Dispose();
				give.Dispose();
			}
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
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("external.example.com", "mx.external.example.com", 10);
			_dns.AddIp("mx.external.example.com", IPAddress.Parse("10.20.30.40"));

			(Stream keep, Stream give) = PairedStream.Create();
			using (var reader = new StreamReader(keep))
			{
				byte[] responseBytes = Encoding.ASCII.GetBytes(responseMessages);
				await keep.WriteAsync(responseBytes, 0, responseBytes.Length);

				Assert.Empty(
					await _transfer.TrySendMailsToStream(
						"external.example.com",
						new[] {mockMailReference},
						new UnclosableStream(give),
						CancellationToken.None));

				Assert.Equal(0, _queue.References.Count);
				Assert.Equal(1, _queue.DeletedReferences.Count);
				await AssertCommandRecieved(reader, "EHLO");
				await AssertCommandRecieved(reader, "HELO");
				await AssertCommandRecieved(reader, "MAIL");
				await AssertCommandRecieved(reader, "RCPT");
				await AssertCommandRecieved(reader, "DATA");

				give.Dispose();
			}
		}

		[Fact]
		public async Task ReadMultiResponse()
		{
			var responseMessages = @"250-STARTTLS
250 example.com greets test.example.com
";
			using (var outStream = new MemoryStream())
			using (var inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (var reader = new StreamReader(inStream))
			using (var writer = new StreamWriter(outStream))
			{
				SmtpResponse response = await _transfer.ExecuteRemoteCommandAsync(writer, reader, "EHLO test.example.com");
				Assert.Equal("EHLO test.example.com" + Environment.NewLine, Encoding.ASCII.GetString(outStream.ToArray()));
				Assert.Equal(SmtpReplyCode.Okay, response.Code);
				Assert.Equal(new[] {"STARTTLS", "example.com greets test.example.com"}, response.Lines);
			}
		}

		[Fact]
		public async Task ReadSingleResponse()
		{
			var responseMessages = @"250 example.com greets test.example.com
";
			using (var outStream = new MemoryStream())
			using (var inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (var reader = new StreamReader(inStream))
			using (var writer = new StreamWriter(outStream))
			{
				SmtpResponse response = await _transfer.ExecuteRemoteCommandAsync(writer, reader, "HELO test.example.com");
				Assert.Equal("HELO test.example.com" + Environment.NewLine, Encoding.ASCII.GetString(outStream.ToArray()));
				Assert.Equal(SmtpReplyCode.Okay, response.Code);
				Assert.Equal(new[] {"example.com greets test.example.com"}, response.Lines);
			}
		}

		[Fact]
		public async Task RelayDomainOverride()
		{
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@relay.example.com",
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);
			_dns.AddMx("relay.example.com", "second.example.com", 20);
			_dns.AddMx("relay.example.com", "first.example.com", 10);
			_dns.AddMx("relay.example.com", "third.example.com", 30);

			_dns.AddIp("first.example.com", IPAddress.Parse("10.0.0.1"));
			_dns.AddIp("second.example.com", IPAddress.Parse("10.0.0.2"));
			_dns.AddIp("third.example.com", IPAddress.Parse("10.0.0.3"));
			_dns.AddIp("relaytarget.example.com", IPAddress.Parse("10.0.0.99"));

			Task executeTask = Task.Run(
				() => _transfer.SendMailsToDomain("relay.example.com", new[] {mockMailReference}, CancellationToken.None));

			MockTcpConnectionProvider.MockTcpClient client = await GetClientFor(IPAddress.Parse("10.0.0.99"));
			Assert.Equal(99, client.Port);
			await WriteToAsync(
				client.HalfStream,
				@"220 example.com greets test.example.com (HELO)
250 Ok (MAIL)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
");

			await executeTask;
			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);
		}

		[Fact]
		public void RepeatMailIsNotReadyYet()
		{
			_failures.AddFailure("mock-1", DateTimeOffset.UtcNow, 5);
			Assert.False(
				_transfer.IsReadyToSend(
					new MockMailReference(
						"mock-1",
						"test@test.example.com",
						new[] {"test@external.example.com"}.ToImmutableList(),
						true,
						_queue)));
		}

		[Fact]
		public async Task SendMultiple()
		{
			var responseMessages = @"250 Ok (MAIL)
250 Ok (RCPT)
250 Ok (RCPT)
354 End data with <CR><LF>.<CR><LF> (DATA)
250 Ok (DATA with .)
250 Bye (QUIT)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] {"test@external.example.com", "other@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);

			using (var outStream = new MemoryStream())
			using (var inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (var reader = new StreamReader(inStream))
			using (var writer = new StreamWriter(outStream))
			{
				Assert.True(await _transfer.TrySendSingleMailAsync(mockMailReference, writer, reader, CancellationToken.None));
			}

			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);
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
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);

			using (var outStream = new MemoryStream())
			using (var inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (var reader = new StreamReader(inStream))
			using (var writer = new StreamWriter(outStream))
			{
				Assert.True(await _transfer.TrySendSingleMailAsync(mockMailReference, writer, reader, CancellationToken.None));
			}

			Assert.Equal(0, _queue.References.Count);
			Assert.Equal(1, _queue.DeletedReferences.Count);
		}

		[Fact]
		public async Task SendSingle_Fail()
		{
			var responseMessages = @"250 Ok (MAIL)
554 Fail (RCPT)
";
			var mockMailReference = new MockMailReference(
				"mock-1",
				"test@test.example.com",
				new[] {"test@external.example.com"}.ToImmutableList(),
				true,
				"Some text",
				_queue);
			_queue.References.Add(mockMailReference);

			using (var outStream = new MemoryStream())
			using (var inStream = new MemoryStream(Encoding.ASCII.GetBytes(responseMessages)))
			using (var reader = new StreamReader(inStream))
			using (var writer = new StreamWriter(outStream))
			{
				Assert.False(await _transfer.TrySendSingleMailAsync(mockMailReference, writer, reader, CancellationToken.None));
			}

			Assert.Equal(1, _queue.References.Count);
			Assert.Equal(0, _queue.DeletedReferences.Count);
		}

		[Fact]
		public void ShouldAttemptRetryAfterSingleFailure()
		{
			Assert.NotNull(
				_transfer.ShouldAttemptRedelivery(
					new MockMailReference(
						"mock-1",
						"test@test.example.com",
						new[] {"test@external.example.com"}.ToImmutableList(),
						true,
						_queue)));
			SmtpFailureData failure = _failures.GetFailure("mock-1", false);
			Assert.NotNull(failure);
			Assert.InRange(failure.FirstFailure, DateTimeOffset.UtcNow - TimeSpan.FromSeconds(1), DateTime.UtcNow);
		}

		[Fact]
		public void ShouldNotAttemptRetryAfterManyFailure()
		{
			_failures.AddFailure("mock-1", DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(10)), 100);
			Assert.Null(
				_transfer.ShouldAttemptRedelivery(
					new MockMailReference(
						"mock-1",
						"test@test.example.com",
						new[] {"test@external.example.com"}.ToImmutableList(),
						true,
						_queue)));
		}
	}
}
