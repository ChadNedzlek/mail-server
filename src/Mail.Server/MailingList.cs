using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailingListProvider
	{
		IEnumerable<string> ExpandMailingList(string mailbox);
	}

	public static class MailingListProvider
	{
		public static IEnumerable<string> ExpandMailingLists(this IMailingListProvider provider, IEnumerable<string> mailboxes)
		{
			var expanded = mailboxes.Select(b => new { original = b, expanded = provider.ExpandMailingList(b) }).ToList();
			if (expanded.Any(x => x.expanded != null))
			{
				return expanded.SelectMany(r => r.expanded ?? new[] { r.original });
			}

			return null;
		}
	}
}
