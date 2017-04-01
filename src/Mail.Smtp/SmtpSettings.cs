namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(
			int[] ports = null,
			string domainName = null,
			string mailStorePath = null,
			string userPasswordFile = null,
			string domainSettingsPath = null,
			string[] relayDomains = null,
			string passwordAlgorithm = null)
			: base(ports, domainName, userPasswordFile, passwordAlgorithm)
		{
			MailStorePath = mailStorePath;
			RelayDomains = relayDomains;
			DomainSettingsPath = domainSettingsPath;
		}

		public string MailStorePath { get; }
		public string[] RelayDomains { get; }
		public string DomainSettingsPath { get; }
	}
}