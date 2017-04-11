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
}