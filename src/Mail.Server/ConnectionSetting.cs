using JetBrains.Annotations;

namespace Vaettir.Mail.Server
{
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	public class ConnectionSetting
	{
		public ConnectionSetting(string protocol, int port, string certificate = null, bool ssl = false)
		{
			Port = port;
			Certificate = certificate;
			Ssl = ssl;
			Protocol = protocol;
		}

		public string Protocol { get; }
		public int Port { get; }
		public string Certificate { get; }
		public bool Ssl { get; }
	}
}
