using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public static class MailUtilities
	{
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

		private static readonly Regex _headerRegex = new Regex(@"^(\w+):(.*)$");
		private static readonly Regex _continutationRegex = new Regex(@"^(\s+.*)$");

		public static async Task<IDictionary<string, IEnumerable<string>>> ParseHeadersAsync(Stream mailStream, CancellationToken token)
		{
			string existingHeaderName = null;
			string existingHeaderValue = null;
			using (StreamReader reader = new StreamReader(mailStream, Encoding.UTF8, false, 1023, true))
			{
				Dictionary<string, IEnumerable<string>> headers = new Dictionary<string, IEnumerable<string>>();
				string line;
				while ((line = await reader.ReadLineAsync().WithCancellation(token)) != null)
				{
					if (existingHeaderName != null)
					{
						var match = _continutationRegex.Match(line);
						if (match.Success)
						{
							existingHeaderValue += match.Groups[1].Value;
							continue;
						}
					}

					{
						var match = _headerRegex.Match(line);
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

					if (String.IsNullOrWhiteSpace(line))
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
			IEnumerable<string> enumerable;
			IList<string> list;

			if (!dict.TryGetValue(key, out enumerable) ||
				(list = enumerable as IList<string>) == null)
			{
				dict.Add(key, list = new List<string>());
			}

			list.Add(value);
		}
	}
}
