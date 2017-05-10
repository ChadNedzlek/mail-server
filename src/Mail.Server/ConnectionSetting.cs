using JetBrains.Annotations;

namespace Vaettir.Mail.Server
{
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
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
