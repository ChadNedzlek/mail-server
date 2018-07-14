using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Transfer.Test
{
	internal class MockDnsResolve : IDnsResolve
	{
		private readonly Dictionary<string, IPAddress> _ip = new Dictionary<string, IPAddress>();
		private readonly Dictionary<string, List<DnsMxRecord>> _mx = new Dictionary<string, List<DnsMxRecord>>();

		public Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token)
		{
			return Task.FromResult(_mx.TryGetValue(domain, out List<DnsMxRecord> mx) ? (IEnumerable<DnsMxRecord>) mx : null);
		}

		public Task<IPAddress> QueryIp(string domain, CancellationToken token)
		{
			return Task.FromResult(_ip.TryGetValue(domain, out IPAddress ip) ? ip : null);
		}

		public void AddMx(string domain, string exchange, int priority)
		{
			if (!_mx.TryGetValue(domain, out List<DnsMxRecord> records))
			{
				_mx.Add(domain, records = new List<DnsMxRecord>());
			}

			records.Add(new DnsMxRecord(exchange, priority));
		}

		public void AddIp(string domain, IPAddress addr)
		{
			_ip.Add(domain, addr);
		}
	}
}
