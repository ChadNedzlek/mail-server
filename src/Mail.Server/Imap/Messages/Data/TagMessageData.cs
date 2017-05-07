using System.Collections.Generic;
using System.Collections.Immutable;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class TagMessageData : BaseListMessageData
	{
		public TagMessageData(ImmutableList<IMessageData> items) : base(items)
		{
		}

		public TagMessageData(IEnumerable<IMessageData> data) : base(data)
		{
		}

		public TagMessageData(params IMessageData[] data) : base((IEnumerable<IMessageData>) data)
		{
		}

		protected override string StartMarker => "[";
		protected override string EndMarker => "]";
	}
}