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
		private MockMailTransferQueue _queue;
		private MockVolatile<SmtpSettings> _settings;
		private MockDnsResolve _dns;
		private MockMailSendFailureManager _failures;
		private MockTcpConnectionProvider _tcp;
		private MailTransfer _transfer;

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
	}
}
