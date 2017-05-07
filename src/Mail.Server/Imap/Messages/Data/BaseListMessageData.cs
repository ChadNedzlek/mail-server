using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Vaettir.Mail.Server.Imap.Messages.Data
{
	public abstract class BaseListMessageData : IMessageData
	{
		public BaseListMessageData(ImmutableList<IMessageData> items)
		{
			Items = items;
		}

		public BaseListMessageData(IEnumerable<IMessageData> data)
		{
			Items = ImmutableList.CreateRange(data);
		}

		public BaseListMessageData(params IMessageData[] data) : this((IEnumerable<IMessageData>) data)
		{
		}

		public ImmutableList<IMessageData> Items { get; }
		protected abstract string StartMarker { get; }
		protected abstract string EndMarker { get; }

		public string ToMessageString()
		{
			var builder = new StringBuilder();
			builder.Append(StartMarker);

			var first = true;

			foreach (IMessageData item in Items)
			{
				if (!first)
				{
					builder.Append(" ");
				}
				first = false;
				builder.Append(item.ToMessageString());
			}
			builder.Append(EndMarker);

			return builder.ToString();
		}
	}
}