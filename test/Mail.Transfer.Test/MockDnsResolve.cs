using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Transfer;

namespace Mail.Transfer.Test
{
	internal class MockDnsResolve : IDnsResolve
	{
		public Dictionary<string, List<DnsMxRecord>> _mx = new Dictionary<string, List<DnsMxRecord>>();
		public Dictionary<string, IPAddress> _ip = new Dictionary<string, IPAddress>();

		public Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token)
		{
			return Task.FromResult(_mx.TryGetValue(domain, out var mx) ? (IEnumerable<DnsMxRecord>) mx : null);
		}

		public Task<IPAddress> QueryIp(string domain, CancellationToken token)
		{
			return Task.FromResult(_ip.TryGetValue(domain, out var ip) ? ip : null);
		}

		public void AddMx(string domain, string exchange, int priority)
		{
			if (!_mx.TryGetValue(domain, out var records))
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