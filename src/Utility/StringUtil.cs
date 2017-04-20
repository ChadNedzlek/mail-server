using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Vaettir.Utility
{
	public static class StringUtil
	{
		public static IList<string> SplitQuoted(
			this string value,
			char delimiter,
			char quoteChar,
			char escapeChar,
			StringSplitOptions options)
		{
			// Remove the escaped and quoted characters, since we don't want to split on those, but leave
			// equal length replacements behind so the indexes line up.
			string stranged = new Regex($@"\{escapeChar}.").Replace(value, $"{escapeChar}\0");
			stranged =
				new Regex($@"\{quoteChar}([^\{quoteChar}]|\{escapeChar}.)*\{quoteChar}").Replace(
					stranged,
					m => new string('\0', m.Length));
			Debug.Assert(stranged.Length == value.Length);
			var parts = new List<string>();
			int oldIndex = -1;
			int index = -1;

			// use the indext from the mangled string...
			while ((index = stranged.IndexOf(delimiter, index + 1)) != -1)
			{
				// but substring the original (to restore the masked out characters)
				string part = value.Substring(oldIndex + 1, index - oldIndex - 1);
				if (part.Length > 0 || !options.HasFlag(StringSplitOptions.RemoveEmptyEntries))
				{
					parts.Add(part);
				}
				oldIndex = index;
			}

			{
				string part = value.Substring(oldIndex + 1);
				if (part.Length > 0 || !options.HasFlag(StringSplitOptions.RemoveEmptyEntries))
				{
					parts.Add(part);
				}
			}

			return parts;
		}
	}
}
