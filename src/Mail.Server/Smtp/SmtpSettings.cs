namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(
			SmtpIncomingMailScan incomingScan = null,
			string workingDirectory = null,
			int[] ports = null,
			string domainName = null,
			string[] domainAliases = null,
			SmtpAcceptDomain[] localDomains = null,
			string mailIncomingQueuePath = null,
			string mailOutgoingQueuePath = null,
			string mailLocalPath = null,
			string userPasswordFile = null,
			string domainSettingsPath = null,
			SmtpRelayDomain[] relayDomains = null,
			string passwordAlgorithm = null,
			int? idleDelay = null)
			: base(ports, domainName, domainAliases, userPasswordFile, passwordAlgorithm)
		{
			IncomingScan = incomingScan;
			WorkingDirectory = workingDirectory;
			LocalDomains = localDomains;
			MailIncomingQueuePath = mailIncomingQueuePath;
			MailOutgoingQueuePath = mailOutgoingQueuePath;
			MailLocalPath = mailLocalPath;
			RelayDomains = relayDomains;
			IdleDelay = idleDelay;
			DomainSettingsPath = domainSettingsPath;
		}

		public SmtpAcceptDomain[] LocalDomains { get; }
		public string MailIncomingQueuePath { get; }
		public string MailOutgoingQueuePath { get; }
		public SmtpRelayDomain[] RelayDomains { get; }
		public string DomainSettingsPath { get; }
		public int? IdleDelay { get; }
		public string WorkingDirectory { get; }
		public SmtpIncomingMailScan IncomingScan { get; }
		public string MailLocalPath { get; }
	}

	public class SmtpIncomingMailScan
	{
		public string SpamAssassinPath { get; }
	}

	public class SmtpAcceptDomain
	{
		public SmtpAcceptDomain(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}

	public class SmtpRelayDomain
	{
		public SmtpRelayDomain(string name, string relay, int? port = null)
		{
			Name = name;
			Relay = relay;
			Port = port;
		}

		public string Name { get; }
		public string Relay { get; }
		public int? Port { get; }
	}
}
