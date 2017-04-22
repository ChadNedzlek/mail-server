using System;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSettings : ProtocolSettings
	{
		public SmtpSettings(
			// Required settings for basic functionality
			string domainName,
			string mailIncomingQueuePath,
			string mailOutgoingQueuePath,
			string workingDirectory,
			SmtpAcceptDomain[] localDomains,
			string domainSettingsPath,

			// Optional settings
			string mailLocalPath = null,
			SmtpIncomingMailScan incomingScan = null,
			int[] ports = null,
			string[] domainAliases = null,
			string userPasswordFile = null,
			SmtpRelayDomain[] relayDomains = null,
			string passwordAlgorithm = null,
			int? idleDelay = null,
			MailDescriminator sendBounce = MailDescriminator.None)
			: base(ports, domainName, domainAliases, userPasswordFile, passwordAlgorithm)
		{
			IncomingScan = incomingScan;
			LocalDomains = localDomains;
			MailIncomingQueuePath = mailIncomingQueuePath;
			MailOutgoingQueuePath = mailOutgoingQueuePath;
			WorkingDirectory = workingDirectory;
			MailLocalPath = mailLocalPath;
			RelayDomains = relayDomains;
			IdleDelay = idleDelay;
			DomainSettingsPath = domainSettingsPath;
			SendBounce = sendBounce;
		}

		public SmtpAcceptDomain[] LocalDomains { get; }
		public string MailIncomingQueuePath { get; }
		public string MailOutgoingQueuePath { get; }
		public SmtpRelayDomain[] RelayDomains { get; }
		public string DomainSettingsPath { get; }
		public int? IdleDelay { get; }
		public SmtpIncomingMailScan IncomingScan { get; }
		public string MailLocalPath { get; }
		public MailDescriminator SendBounce { get; }
		public string WorkingDirectory { get; }
	}

	[Flags]
	public enum MailDescriminator
	{
		None = 0,
		Internal = 0b01,
		External = 0b10,
		Both = Internal | External,
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
