using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;

namespace Vaettir.Mail.Server
{
	public class DnsClientResolver : IDnsResolve
	{
		private readonly LookupClient _lookup;

		public DnsClientResolver()
		{
			_lookup = new LookupClient();
		}

		public async Task<IEnumerable<DnsMxRecord>> QueryMx(string domain, CancellationToken token)
		{
			IDnsQueryResponse response = await _lookup.QueryAsync(domain, QueryType.MX, token);
			return response.Answers.MxRecords().Select(TranformMxRecord);
		}

		public async Task<IPAddress> QueryIp(string domain, CancellationToken token)
		{
			IDnsQueryResponse response = await _lookup.QueryAsync(domain, QueryType.A, token);
			ARecord aRecord = response.Answers.ARecords().FirstOrDefault();
			if (aRecord != null)
			{
				return aRecord.Address;
			}

			response = await _lookup.QueryAsync(domain, QueryType.AAAA, token);
			AaaaRecord aaaaRecord = response.Answers.AaaaRecords().FirstOrDefault();
			if (aaaaRecord != null)
			{
				return aaaaRecord.Address;
			}

			return null;
		}

		private static DnsMxRecord TranformMxRecord(MxRecord lookupMx)
		{
			return new DnsMxRecord(lookupMx.Exchange, lookupMx.Preference);
		}
	}
}
