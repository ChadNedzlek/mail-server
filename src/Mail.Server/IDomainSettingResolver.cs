using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public interface IDomainSettingResolver
	{
		IVolatile<DomainSettings> GetDomainSettings(string domain);
	}
}
