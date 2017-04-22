using System.Collections.Generic;

namespace Vaettir.Mail.Server
{
	public class DistributionList
	{
		public DistributionList(
			string mailbox = null,
			string description = null,
			IList<string> owners = null,
			IList<string> members = null,
			bool allowExternalSenders = false,
			bool enabled = false)
		{
			Mailbox = mailbox;
			Description = description;
			Owners = owners;
			Members = members;
			AllowExternalSenders = allowExternalSenders;
			Enabled = enabled;
		}

		public string Mailbox { get; }
		public string Description { get; }
		public IList<string> Owners { get; }
		public IList<string> Members { get; }
		public bool AllowExternalSenders { get; }
		public bool Enabled { get; }
	}
}