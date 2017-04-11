using System;

namespace Vaettir.Mail.Server
{
	public class SmtpFailureData
	{
		public SmtpFailureData(string messageId)
		{
			MessageId = messageId;
		}

		public string MessageId { get; }
		public DateTimeOffset FirstFailure { get; set; }
		public int Retries { get; set; }
	}
}
