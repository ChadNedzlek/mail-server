namespace Vaettir.Mail.Server
{
	public class ProtocolSettings
	{
		public ProtocolSettings(
			int[] ports = null,
			string domainName = null,
			string[] domainAliases = null,
			string userPasswordFile = null,
			string passwordAlgorithm = null)
		{
			Ports = ports;
			DomainName = domainName;
			UserPasswordFile = userPasswordFile;
			PasswordAlgorithm = passwordAlgorithm;
			DomainAliases = domainAliases;
		}

		public int[] Ports { get; }
		public string DomainName { get; }
		public string[] DomainAliases { get; }
		public string UserPasswordFile { get; }
		public string PasswordAlgorithm { get; }
	}
}