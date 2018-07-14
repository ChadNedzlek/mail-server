using System.IO;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	[Injected]
	public class FileSystemDomainResolver : IDomainSettingResolver
	{
		private readonly IVolatile<AgentSettings> _settings;

		public FileSystemDomainResolver(IVolatile<AgentSettings> settings)
		{
			_settings = settings;
		}

		public IVolatile<DomainSettings> GetDomainSettings(string domain)
		{
			string settingsFileName = Path.Combine(_settings.Value.DomainSettingsPath, domain + ".json");
			return FileWatcherSettings<DomainSettings>.Load(settingsFileName);
		}
	}
}
