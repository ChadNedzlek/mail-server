using System.Collections.Generic;
using System.Collections.Immutable;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public class ListMessageData : BaseListMessageData
	{
		public ListMessageData(ImmutableList<IMessageData> items) : base(items)
		{
		}

		public ListMessageData(IEnumerable<IMessageData> data) : base(data)
		{
		}

		public ListMessageData(params IMessageData[] data) : base((IEnumerable<IMessageData>) data)
		{
		}

		protected override string StartMarker => "(";
		protected override string EndMarker => ")";
	}
}