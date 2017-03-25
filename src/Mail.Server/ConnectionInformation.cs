namespace Vaettir.Mail.Server
{
	public class ConnectionInformation
	{
		public ConnectionInformation(string localAddress, string remoteAddress)
		{
			LocalAddress = localAddress;
			RemoteAddress = remoteAddress;
		}

		public string LocalAddress { get; }
		public string RemoteAddress { get; }

		public string RemoteHost { get; set; }
	}
}
