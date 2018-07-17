namespace Vaettir.Mail.Mime
{
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