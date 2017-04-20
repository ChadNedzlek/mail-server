using System;

namespace Vaettir.Mail.Server
{
	[Flags]
	public enum MailboxFlags
	{
		None = 0,
		Forwarded = 0b0000_0001,
		Answered = 0b0000_0010,
		Seen = 0b0000_0100,
		Deleted = 0b0000_1000,
		Draft = 0b0001_0000,
		Flagged = 0b0010_0000,
	}
}
