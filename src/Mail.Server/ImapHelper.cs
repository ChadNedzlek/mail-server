using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vaettir.Mail.Server
{
	public static class ImapHelper
	{
		private static readonly Dictionary<char, MailboxFlags> s_maildirToInternal = new Dictionary<char, MailboxFlags>
		{
			{'P', MailboxFlags.Forwarded},
			{'R', MailboxFlags.Answered},
			{'S', MailboxFlags.Seen},
			{'T', MailboxFlags.Deleted},
			{'D', MailboxFlags.Draft},
			{'F', MailboxFlags.Flagged},
		};

		private static readonly Dictionary<string, MailboxFlags> s_imapToInternal = new Dictionary<string, MailboxFlags>
		{
			{"$Forwarded", MailboxFlags.Forwarded},
			{"\\Answered", MailboxFlags.Answered},
			{"\\Seen", MailboxFlags.Seen},
			{"\\Deleted", MailboxFlags.Deleted},
			{"\\Draft", MailboxFlags.Draft},
			{"\\Flagged", MailboxFlags.Flagged},
		};

		private static readonly Dictionary<MailboxFlags, char> s_internalToMaildir = Enumerable.ToDictionary<KeyValuePair<char, MailboxFlags>, MailboxFlags, char>(s_maildirToInternal, p => p.Value, p => p.Key);
		private static readonly Dictionary<MailboxFlags, string> s_internalToImap = Enumerable.ToDictionary<KeyValuePair<string, MailboxFlags>, MailboxFlags, string>(s_imapToInternal, p => p.Value, p => p.Key);

		public static MailboxFlags GetFlagsFromMailDir(string maildirFlags)
		{
			MailboxFlags flags = MailboxFlags.None;
			foreach (var c in maildirFlags)
			{
				if (s_maildirToInternal.TryGetValue(c, out var f))
				{
					flags |= f;
				}
			}
			return flags;
		}

		public static string GetMailDirFromFlags(MailboxFlags flags)
		{
			return new String(s_internalToMaildir.Where(p => flags.HasFlag(p.Key)).Select(p => p.Value).ToArray());
		}
	}
}