using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Transfer;

namespace Mail.Transfer.Test
{
	internal class MockDnsResolve : IDnsResolve
	{
		public Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task<IPAddress> QueryIp(string domain, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}
	}
}