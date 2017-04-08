using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Vaettir.Mail.Server
{
	public abstract class MailWriteReference : IMailWriteReference
	{
		protected MailWriteReference(string id, string sender, IEnumerable<string> recipients, IMailStore store)
		{
			Id = id;
			Sender = sender;
			Store = store;
			Recipients = ImmutableList.CreateRange(recipients);
		}

		public string Id { get; }
		public string Sender { get;  }
		public IImmutableList<string> Recipients { get; }
		public IMailStore Store { get; }

		public abstract Stream BodyStream { get; }
		public abstract void Dispose();
	}
}