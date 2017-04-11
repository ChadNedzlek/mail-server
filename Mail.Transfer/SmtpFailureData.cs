using System;

namespace Vaettir.Mail.Transfer
{
	public class SmtpFailureData
	{
		public SmtpFailureData(string messageId)
		{
			MessageId = messageId;
		}

		public string MessageId { get; }
		public DateTimeOffset LastAttempt { get; set; }
		public int Retries { get; set; }
	}
}
