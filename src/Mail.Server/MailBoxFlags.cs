using System;

namespace Vaettir.Mail.Server
{
	[Flags]
	public enum MailboxFlags
	{
		None = 0,
		Forwarded = 0b00000001,
		Answered = 0b00000010,
		Seen = 0b00000100,
		Deleted = 0b00001000,
		Draft = 0b00010000,
		Flagged = 0b00100000,
	}
}