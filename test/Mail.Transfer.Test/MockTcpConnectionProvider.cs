using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace Vaettir.Mail.Transfer.Test
{
	internal sealed class MockTcpConnectionProvider : ITcpConnectionProvider, IDisposable
	{
		public readonly List<MockTcpClient> Created = new List<MockTcpClient>();

		public void Dispose()
		{
			foreach (MockTcpClient item in Created)
			{
				item.HalfStream?.Dispose();
				item.ActiveStream?.Dispose();
				item.Dispose();
			}
		}

		public ITcpClient GetClient()
		{
			var c = new MockTcpClient();
			Created.Add(c);
			return c;
		}

		public class MockTcpClient : ITcpClient
		{
			public IPAddress IpAddress { get; private set; }
			public int Port { get; private set; }
			public bool IsOpen { get; private set; }
			public Stream HalfStream { get; private set; }
			public Stream ActiveStream { get; private set; }

			public void Dispose()
			{
				IsOpen = false;
			}

			public Task ConnectAsync(IPAddress targetIp, int port)
			{
				IsOpen = true;
				IpAddress = targetIp;
				Port = port;
				return Task.CompletedTask;
			}

			public Stream GetStream()
			{
				var (a, b) = PairedStream.Create();
				HalfStream = a;
				ActiveStream = b;
				return b;
			}
		}
	}
}
