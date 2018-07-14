using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	[Injected]
	public class DnsClientResolver : IDnsResolve
	{
		private readonly LookupClient _lookup;

		public DnsClientResolver()
		{
			_lookup = new LookupClient();
		}

		public async Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token)
		{
			IDnsQueryResponse response = await _lookup.QueryAsync(domain, QueryType.MX, QueryClass.IN, token);
			return response.Answers.MxRecords().Select(TranformMxRecord);
		}

		public async Task<IPAddress> QueryIp(string domain, CancellationToken token)
		{
			IDnsQueryResponse response = await _lookup.QueryAsync(domain, QueryType.A, QueryClass.IN, token);
			ARecord aRecord = response.Answers.ARecords().FirstOrDefault();
			if (aRecord != null)
			{
				return aRecord.Address;
			}

			response = await _lookup.QueryAsync(domain, QueryType.AAAA, QueryClass.IN, token);
			AaaaRecord aaaaRecord = response.Answers.AaaaRecords().FirstOrDefault();
			return aaaaRecord?.Address;
		}

		private static DnsMxRecord TranformMxRecord(MxRecord lookupMx)
		{
			return new DnsMxRecord(lookupMx.Exchange, lookupMx.Preference);
		}
	}
}
