using System.Collections.Generic;

namespace Vaettir.Mail.Server
{
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
}
