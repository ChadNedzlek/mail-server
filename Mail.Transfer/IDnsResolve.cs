using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;

namespace Vaettir.Mail.Transfer
{
	public interface IDnsResolve
	{
		Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token);
		Task<IPAddress> QueryIp(string domain, CancellationToken token);
	}
}