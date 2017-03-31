using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(int[] ports, string domainName, string mailStorePath, string[] relayDomains, string userPasswordFile) : base(ports, domainName, userPasswordFile)
		{
			MailStorePath = mailStorePath;
			RelayDomains = relayDomains;
		}

	    public string MailStorePath { get; }
		public string[] RelayDomains { get; }
	}
}