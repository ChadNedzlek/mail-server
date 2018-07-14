using System;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class LiteralMessageData : IMessageData
	{
		public LiteralMessageData(byte[] dataBytes, int length)
		{
			Data = new byte[length];
			Array.Copy(dataBytes, Data, length);
		}

		public byte[] Data { get; }

		public string ToMessageString()
		{
			throw new NotSupportedException();
		}
	}
}
