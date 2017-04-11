using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Vaettir.Mail.Transfer
{
	public class WrappedTcpClientProvider : ITcpConnectionProvider
	{
		public ITcpClient GetClient()
		{
			return new WrappedTcpClient();
		}

		private class WrappedTcpClient : ITcpClient
		{
			private readonly TcpClient _client;

			public WrappedTcpClient()
			{
				_client = new TcpClient();
			}

			public void Dispose()
			{
				_client?.Dispose();
			}

			public Task ConnectAsync(IPAddress targetIp, int port)
			{
				return _client.ConnectAsync(targetIp, port);
			}

			public Stream GetStream()
			{
				return _client.GetStream();
			}
		}
	}
}