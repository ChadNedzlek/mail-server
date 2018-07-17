using System.Diagnostics;

namespace Vaettir.Mail.Mime
{
	[DebuggerDisplay("{Start,nq}-{End,nq}")]
	public class MessageSpan
	{
		public long Start { get; }
		public long Length { get; }

		public MessageSpan(long start, long length)
		{
			Start = start;
			Length = length;
		}

		public long End => Start + Length;
	}
}
