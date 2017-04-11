using System;
using System.Collections.Generic;
using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Transfer
{
	public class SmtpResponse
	{
		public SmtpResponse(ReplyCode code, List<string> lines)
		{
			Code = code;
			Lines = lines;
		}

		public ReplyCode Code { get; }
		public List<string> Lines { get; }
	}

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
