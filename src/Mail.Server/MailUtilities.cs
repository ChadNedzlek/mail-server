using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public static class MailUtilities
	{
		private const string DottedAtom = "[-a-zA-Z0-9!#$%&'*+-/=?^_`{|}~.]";

		private static readonly Regex s_headerRegex = new Regex(@"^(\w+):(.*)$");
		private static readonly Regex s_continutationRegex = new Regex(@"^(\s+.*)$");
		private static readonly Regex s_addressPart = new Regex($@"<?\s*({DottedAtom}*@{DottedAtom}*)\s*>?$");

		public static string GetDomainFromMailbox(string mailbox)
		{
			int atIndex = mailbox.LastIndexOf('@');
			if (atIndex == -1)
			{
				return null;
			}

			return mailbox.Substring(atIndex + 1);
		}

		public static string GetNameFromMailbox(string mailbox)
		{
			int atIndex = mailbox.LastIndexOf('@');
			if (atIndex == -1)
			{
				return null;
			}

			return mailbox.Substring(0, atIndex);
		}

		public static async Task<IDictionary<string, IEnumerable<string>>> ParseHeadersAsync(Stream mailStream)
		{
			string existingHeaderName = null;
			string existingHeaderValue = null;
			using (var reader = new StreamReader(mailStream, Encoding.UTF8, false, 1023, true))
			{
				var headers = new Dictionary<string, IEnumerable<string>>();
				string line;
				while ((line = await reader.ReadLineAsync()) != null)
				{
					if (existingHeaderName != null)
					{
						Match match = s_continutationRegex.Match(line);
						if (match.Success)
						{
							existingHeaderValue += match.Groups[1].Value;
							continue;
						}
					}

					{
						Match match = s_headerRegex.Match(line);
						if (match.Success)
						{
							if (existingHeaderName != null)
							{
								AddItem(headers, existingHeaderName, existingHeaderValue);
							}

							existingHeaderName = match.Groups[1].Value;
							existingHeaderValue = match.Groups[2].Value;
						}
					}

					if (string.IsNullOrWhiteSpace(line))
					{
						break;
					}
				}

				if (existingHeaderName != null)
				{
					AddItem(headers, existingHeaderName, existingHeaderValue);
				}

				return headers;
			}
		}

		private static void AddItem(Dictionary<string, IEnumerable<string>> dict, string key, string value)
		{
			IList<string> list;

			if (!dict.TryGetValue(key, out IEnumerable<string> enumerable) ||
				(list = enumerable as IList<string>) == null)
			{
				dict.Add(key, list = new List<string>());
			}

			list.Add(value);
		}

		public static string GetMailboxFromAddress(string address, ILogger logger = null)
		{
			Match mailboxPart = s_addressPart.Match(address);
			if (!mailboxPart.Success)
			{
				logger?.Warning($"Unable to parse Mailbox: {address}");
				return null;
			}

			return mailboxPart.Groups[1].Value;
		}
	}
}
