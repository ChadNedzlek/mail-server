using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;

namespace Vaettir.Mail.Test.Utilities
{
	public static class TestHelpers
	{
		public static IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> GetAuths()
		{
			return new List<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>>
			{
				new Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>(
					() => new MockEncryptedAuth(),
					new AuthenticationMechanismAttribute("ENC", true)),
				new Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>(
					() => new MockPlainTextAuth(MockPlainTextAuth.Action.Null),
					new AuthenticationMechanismAttribute("PLN", false))
			};
		}

		public static IVariableStreamReader GetReader(string content)
		{
			return new VariableStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
		}

		public static X509Certificate2 GetSelfSigned()
		{
			// This is a self-signed cert for test.vaettir.net good until 2044
			return
				new X509Certificate2(
					Convert.FromBase64String(
						"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"),
					string.Empty);
		}

		public static AgentSettings MakeSettings(
			string domainName = null,
			SmtpAcceptDomain[] localDomains = null,
			SmtpIncomingMailScan incomingScan = null,
			ConnectionSetting[] connections = null,
			string[] domainAliases = null,
			string userPasswordFile = null,
			SmtpRelayDomain[] relayDomains = null,
			string passwordAlgorithm = null,
			int? idleDelay = null,
			MailDescriminator sendBounce = MailDescriminator.None,
			int unauthenticatedMessageSizeLimit = 0)
		{
			return new AgentSettings(
				domainName,
				mailIncomingQueuePath: null,
				mailOutgoingQueuePath: null,
				workingDirectory: null,
				localDomains: localDomains,
				connections: connections,
				domainSettingsPath: null,
				mailLocalPath: null,
				incomingScan: incomingScan,
				domainAliases: domainAliases,
				userPasswordFile: userPasswordFile,
				relayDomains: relayDomains,
				passwordAlgorithm: passwordAlgorithm,
				idleDelay: idleDelay,
				sendBounce: sendBounce,
				unauthenticatedMessageSizeLimit: unauthenticatedMessageSizeLimit);
		}
	}
}
