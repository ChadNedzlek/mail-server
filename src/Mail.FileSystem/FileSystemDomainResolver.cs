using System.IO;
using JetBrains.Annotations;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
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
