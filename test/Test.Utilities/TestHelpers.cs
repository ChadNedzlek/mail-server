using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;

namespace Vaettir.Mail.Test.Utilities
{
	public static class TestHelpers
	{
		public static IEnumerable<Lazy<IAuthenticationSession, AuthencticationMechanismMetadata>> GetAuths()
		{
			return new List<Lazy<IAuthenticationSession, AuthencticationMechanismMetadata>>
			{
				new Lazy<IAuthenticationSession, AuthencticationMechanismMetadata>(
					() => new MockEncryptedAuth(),
					new AuthencticationMechanismMetadata{Name="ENC", RequiresEncryption = true}),
				new Lazy<IAuthenticationSession, AuthencticationMechanismMetadata>(
					() => new MockPlainTextAuth(MockPlainTextAuth.Action.Null),
					new AuthencticationMechanismMetadata{Name="PLN", RequiresEncryption = false})
			};
		}

		public static IVariableStreamReader GetReader(string content)
		{
			return new VariableStreamReader(new MemoryStream(Encoding.UTF8.GetBytes(content)));
		}

		public static X509Certificate2 GetSelfSigned()
		{
			CertificateRequest req = new CertificateRequest(
				"CN=vaettir.net.test",
				RSA.Create(2048),
				HashAlgorithmName.SHA256,
				RSASignaturePadding.Pkcs1
			);

			using var temp = req.CreateSelfSigned(DateTimeOffset.Now - TimeSpan.FromDays(7), DateTimeOffset.Now + TimeSpan.FromDays(7));
			// Windows SslStream is terrible, and can't accept ephemeral keys, and export/import causes it not to be that
			return new X509Certificate2(temp.Export(X509ContentType.Pkcs12));
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
			MailDiscriminator sendBounce = MailDiscriminator.None,
			int unauthenticatedMessageSizeLimit = 0,
			string mailLocalPath = null)
		{
			return new AgentSettings(
				domainName: domainName,
				mailIncomingQueuePath: null,
				mailOutgoingQueuePath: null,
				workingDirectory: null,
				localDomains: localDomains,
				connections: connections,
				domainSettingsPath: null,
				mailLocalPath: mailLocalPath,
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
