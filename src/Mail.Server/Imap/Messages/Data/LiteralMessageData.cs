using System;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class LiteralMessageData : IMessageData
	{
		public LiteralMessageData(int length)
		{
			Length = length;
		}

		public int Length { get; }

		public string ToMessageString()
		{
			throw new NotSupportedException();
		}
	}
}
