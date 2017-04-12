using System;
using System.Collections.Immutable;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Mail.Transfer;
using Vaettir.Utility;
using Xunit;

namespace Mail.Transfer.Test
{
	public class MailTransferTests
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
	}
}
