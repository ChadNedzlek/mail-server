using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
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
		
		public static string ToHex(this byte[] bytes)
		{
			StringBuilder builder = new StringBuilder(bytes.Length * 2);

			for (int i = 0; i < bytes.Length; i++)
			{
				builder.Append(bytes[i].ToString("X2"));
			}

			return builder.ToString();
		}

		public static byte[] FromHex(this string hexString)
		{
			byte[] bytes = new byte[hexString.Length / 2];

			for (int i = 0; i < hexString.Length; i += 2)
			{
				string s = hexString.Substring(i, 2);
				bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
			}

			return bytes;
		}
	}
}
