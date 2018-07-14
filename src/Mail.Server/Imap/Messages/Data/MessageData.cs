using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public static class MessageData
	{
		private static readonly Regex ValidAtom = new Regex(@"^[a-z0-9_-/\\.!/@#$%^&*]+$", RegexOptions.IgnoreCase);
		private static readonly Regex ValidQuote = new Regex(@"^[a-z0-9_-/\\.!/@#$%^&* \t]+$", RegexOptions.IgnoreCase);

		public static string GetString(IMessageData data, Encoding encoding)
		{
			var atom = data as AtomMessageData;
			if (atom != null)
			{
				return atom.Value;
			}

			var literal = data as LiteralMessageData;
			if (literal != null)
			{
				return encoding.GetString(literal.Data);
			}

			return null;
		}

		public static bool TryGetDateTime(IMessageData data, Encoding encoding, out DateTime value)
		{
			string stringValue = GetString(data, encoding);
			return DateTime.TryParseExact(
				stringValue,
				"dd-MMM-yyyy HH:mm:ss zzzz",
				CultureInfo.InvariantCulture,
				DateTimeStyles.AdjustToUniversal,
				out value);
		}

		public static IMessageData CreateData(string value)
		{
			if (ValidAtom.IsMatch(value))
			{
				return new AtomMessageData(value);
			}

			if (ValidQuote.IsMatch(value))
			{
				return new QuotedMessageData(value);
			}

			byte[] dataBytes = Encoding.UTF8.GetBytes(value);
			return new LiteralMessageData(dataBytes, dataBytes.Length);
		}
	}
}
