using System.Collections.Generic;

namespace Vaettir.Mail.Server.Smtp
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
}
