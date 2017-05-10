using System.Collections.Generic;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpResponse
	{
		public SmtpResponse(SmtpReplyCode code, List<string> lines)
		{
			Code = code;
			Lines = lines;
		}

		public SmtpReplyCode Code { get; }
		public List<string> Lines { get; }
	}
}
