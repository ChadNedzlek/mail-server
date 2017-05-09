namespace Vaettir.Mail.Server
{
	public class ProtocolSettings
	{
		public ProtocolSettings(
			string domainName,
			ConnectionSetting[] connections,
			string[] domainAliases = null,
			string userPasswordFile = null,
			string passwordAlgorithm = null)
		{
			DomainName = domainName;
			Connections = connections;
			UserPasswordFile = userPasswordFile;
			PasswordAlgorithm = passwordAlgorithm;
			DomainAliases = domainAliases;
		}

		public ConnectionSetting[] Connections { get; }
		public string DomainName { get; }
		public string[] DomainAliases { get; }
		public string UserPasswordFile { get; }
		public string PasswordAlgorithm { get; }
	}

	public class ConnectionSetting
	{
		public ConnectionSetting(string protocol, int port, string certificatePath, bool ssl)
		{
			Port = port;
			CertificatePath = certificatePath;
			Ssl = ssl;
			Protocol = protocol;
		}

		public string Protocol { get; }
		public int Port { get; }
		public string CertificatePath { get; }
		public bool Ssl { get; }
	}
}
