namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(
			int[] ports,
			string domainName,
			string mailStorePath,
			string[] relayDomains,
			string userPasswordFile,
			string passwordAlgorithm)
		: base(ports, domainName, userPasswordFile, passwordAlgorithm)
		{
			MailStorePath = mailStorePath;
			RelayDomains = relayDomains;
		}

		public string MailStorePath { get; }
		public string[] RelayDomains { get; }
	}
}