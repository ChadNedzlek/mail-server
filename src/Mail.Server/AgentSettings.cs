using System;
using System.Collections.Generic;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public class AgentSettings
	{
		public AgentSettings(
			// Required settings for basic functionality
			string domainName,
			ConnectionSetting[] connections,
			string mailIncomingQueuePath,
			string mailOutgoingQueuePath,
			string workingDirectory,
			SmtpAcceptDomain[] localDomains,
			string domainSettingsPath,

			// Optional settings
			string mailLocalPath = null,
			SmtpIncomingMailScan incomingScan = null,
			string[] domainAliases = null,
			string userPasswordFile = null,
			SmtpRelayDomain[] relayDomains = null,
			string passwordAlgorithm = null,
			int? idleDelay = null,
			MailDiscriminator sendBounce = MailDiscriminator.None,
			IDictionary<string, LogSettings> logging = null,
			int unauthenticatedMessageSizeLimit = 0
		)
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
			UnauthenticatedMessageSizeLimit = unauthenticatedMessageSizeLimit;
			SendBounce = sendBounce;
			Logging = logging;
			DomainName = domainName;
			Connections = connections;
			UserPasswordFile = userPasswordFile;
			PasswordAlgorithm = passwordAlgorithm;
			DomainAliases = domainAliases;
		}

		public SmtpAcceptDomain[] LocalDomains { get; }
		public string MailIncomingQueuePath { get; }
		public string MailOutgoingQueuePath { get; }
		public SmtpRelayDomain[] RelayDomains { get; }
		public string DomainSettingsPath { get; }
		public int? IdleDelay { get; }
		public SmtpIncomingMailScan IncomingScan { get; }
		public string MailLocalPath { get; }
		public MailDiscriminator SendBounce { get; }
		public string WorkingDirectory { get; }
		public IDictionary<string, LogSettings> Logging { get; }
		public ConnectionSetting[] Connections { get; }
		public string DomainName { get; }
		public string[] DomainAliases { get; }
		public string UserPasswordFile { get; }
		public string PasswordAlgorithm { get; }
		public int UnauthenticatedMessageSizeLimit { get; }
	}

	[Flags]
	public enum MailDiscriminator
	{
		None = 0,
		Internal = 0b01,
		External = 0b10,
		Both = Internal | External
	}

	public class SmtpIncomingMailScan
	{
		public SmtpIncomingMailScan(SpamAssassinSettings spamAssassin)
		{
			SpamAssassin = spamAssassin;
		}

		public SpamAssassinSettings SpamAssassin{ get; }
	}

	public class SpamAssassinSettings
	{
		public SpamAssassinSettings(string clientPath, double? deleteThreshold, string scoreHeader)
		{
			ClientPath = clientPath;
			DeleteThreshold = deleteThreshold;
			ScoreHeader = scoreHeader;
		}

		public string ClientPath { get; }
		public double? DeleteThreshold { get; }
		public string ScoreHeader { get; }
	}

	public class SmtpAcceptDomain
	{
		public SmtpAcceptDomain(string name, string fallbackMailbox = null)
		{
			Name = name;
			FallbackMailbox = fallbackMailbox;
		}

		public string Name { get; }
		public string FallbackMailbox { get; }
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
