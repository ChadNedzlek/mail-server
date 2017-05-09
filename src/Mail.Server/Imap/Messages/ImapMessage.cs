using System.Collections.Generic;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Messages
{
	public class ImapMessage
	{
		public ImapMessage(string tag, string messageName, IReadOnlyList<IMessageData> data)
			: this(tag, Concat(messageName, data))
		{
			Tag = tag;
			Data = data;
		}

		public ImapMessage(string tag, IReadOnlyList<IMessageData> data)
		{
			Tag = tag;
			Data = data;
		}

		public ImapMessage(string tag, string messageName, params IMessageData[] data)
			: this(tag, messageName, (IReadOnlyList<IMessageData>) data)
		{
		}

		public ImapMessage(string tag, params IMessageData[] data)
			: this(tag, (IReadOnlyList<IMessageData>) data)
		{
		}

		public string Tag { get; }
		public IReadOnlyList<IMessageData> Data { get; }

		private static IReadOnlyList<IMessageData> Concat(string messageName, IReadOnlyList<IMessageData> data)
		{
			var newData = new List<IMessageData> {new AtomMessageData(messageName)};
			newData.AddRange(data);
			return newData;
		}
	}
}