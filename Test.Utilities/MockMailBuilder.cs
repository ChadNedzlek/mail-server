using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockMailBuilder : IMailBuilder
	{
		public SmtpMailMessage PendingMail { get; set; }
	}
}
