using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings
	{
		public SmtpSettings(int[] defaultPorts, string domainName, string mailStorePath, string[] relayDomains)
		{
			DefaultPorts = defaultPorts;
			DomainName = domainName;
			MailStorePath = mailStorePath;
			RelayDomains = relayDomains;
		}

		public int[] DefaultPorts { get; }
		public string DomainName { get; }
		public string MailStorePath { get; }
		public string[] RelayDomains { get; }
	}
}