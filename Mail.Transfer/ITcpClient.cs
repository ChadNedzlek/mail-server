using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Vaettir.Mail.Transfer
{
	public interface ITcpClient : IDisposable
	{
		Task ConnectAsync(IPAddress targetIp, int port);
		Stream GetStream();
	}
}