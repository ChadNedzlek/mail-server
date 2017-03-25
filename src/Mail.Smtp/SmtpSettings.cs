using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(int[] ports, string domainName, string mailStorePath, string[] relayDomains) : base(ports)
		{
			DomainName = domainName;
			MailStorePath = mailStorePath;
			RelayDomains = relayDomains;
		}

		public string DomainName { get; }
		public string MailStorePath { get; }
		public string[] RelayDomains { get; }
	}
}