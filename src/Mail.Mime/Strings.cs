using System;
using System.Text;

namespace Vaettir.Mail.Mime
{
	public static class Strings
	{
		public static readonly Value ContentType = new Value("Content-Type");

		public class Value
		{
			public Value(string stringValue)
			{
				StringValue = stringValue;
				ByteValue = Encoding.UTF8.GetBytes(StringValue).AsMemory();
			}

			public string StringValue { get; }
			public ReadOnlyMemory<byte> ByteValue { get; }

			public bool Equals(ReadOnlySpan<byte> compare)
			{
				return ByteValue.Span.SequenceEqual(compare);
			}

			public bool Starts(ReadOnlySpan<byte> fullText)
			{
				if (fullText.Length < ByteValue.Length)
					return false;
				return fullText.Slice(0, ByteValue.Length).SequenceEqual(ByteValue.Span);
			}
		}
	}
}
