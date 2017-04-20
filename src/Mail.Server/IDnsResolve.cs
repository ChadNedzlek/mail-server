using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IDnsResolve
	{
		Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token);
		Task<IPAddress> QueryIp(string domain, CancellationToken token);
	}
}