using System.Collections.Generic;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public interface IDomainSettingResolver
	{
		IVolatile<DomainSettings> GetDomainSettings(string domain);
	}

	public class DomainSettings
	{
		public DomainSettings(IList<DistributionList> distributionLists = null, IDictionary<string, string> aliases = null)
		{
			DistributionLists = distributionLists;
			Aliases = aliases;
		}

		public IList<DistributionList> DistributionLists { get; }
		public IDictionary<string, string> Aliases { get; }
	}

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
