using System;
using System.Diagnostics;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	[DebuggerDisplay("<ATOM {Value}>")]
	public class AtomMessageData : IMessageData
	{
		public AtomMessageData(string value)
		{
			Value = value;
		}

		public string Value { get; }

		public string ToMessageString()
		{
			return Value;
		}

		public NumberMessageData AsNumber()
		{
			return new NumberMessageData(int.Parse(Value));
		}

		public NumberRangeMessageData AsNumberRange()
		{
			int colonIndex = Value.IndexOf(':');
			if (colonIndex == -1)
			{
				throw new FormatException();
			}

			return new NumberRangeMessageData(
				int.Parse(Value.Substring(0, colonIndex)),
				int.Parse(Value.Substring(colonIndex + 1)));
		}
	}
}
