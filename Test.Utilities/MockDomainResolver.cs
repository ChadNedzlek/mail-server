using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockDomainResolver : IDomainSettingResolver
	{
		public MockDomainResolver(DomainSettings settings)
		{
			Settings = new MockVolatile<DomainSettings>(settings);
		}

		public IVolatile<DomainSettings> GetDomainSettings(string domain)
		{
			return Settings;
		}

		public IVolatile<DomainSettings> Settings { get; }
	}
}