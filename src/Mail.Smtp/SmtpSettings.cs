namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(
			int[] ports = null,
			string domainName = null,
			string mailIncomingQueuePath = null,
			string mailOutgoingQueuePath = null,
			string userPasswordFile = null,
			string domainSettingsPath = null,
			SmtpRelayDomain[] relayDomains = null,
			string passwordAlgorithm = null,
			int? idleDelay = null)
			: base(ports, domainName, userPasswordFile, passwordAlgorithm)
		{
			MailIncomingQueuePath = mailIncomingQueuePath;
			MailOutgoingQueuePath = mailOutgoingQueuePath;
			RelayDomains = relayDomains;
		    IdleDelay = idleDelay;
		    DomainSettingsPath = domainSettingsPath;
		}

		public string MailIncomingQueuePath { get; }
		public string MailOutgoingQueuePath { get; }
		public SmtpRelayDomain[] RelayDomains { get; }
		public string DomainSettingsPath { get; }
	    public int? IdleDelay { get; }
	}

    public class SmtpRelayDomain
    {
        public SmtpRelayDomain(string name, int? port = null)
        {
            Name = name;
            Port = port;
        }

        public string Name { get; }
        public int? Port { get; }
    }
}