using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DnsClient;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Delivery
{
	public class MailDevliery
	{
		private readonly IMailTransferQueue _queue;
		private readonly IVolatile<SmtpSettings> _settings;
		private readonly ILogger _log;

		public MailDevliery(IMailTransferQueue queue, IVolatile<SmtpSettings> settings,  ILogger log)
		{
			_queue = queue;
			_settings = settings;
			_log = log;
		}

		public async Task RunAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					foreach (var domain in _queue.GetMailsByDomain())
					{
						var mails = _queue.GetAllMailForDomain(domain).ToList();
						if (mails.Count == 0)
						{
							continue;
						}
						await SendMailsToDomain(domain, mails, token);
					}
				}
				catch (Exception e)
				{
					_log.Error("Failed processing outgoing mail queues", e);
				}
			}
		}

		private static async Task SendMailsToDomain(string domain, List<IMailReference> mails, CancellationToken token)
		{
			LookupClient dns = new LookupClient();
			IDnsQueryResponse dnsResponse = await dns.QueryAsync(domain, QueryType.MX, token);
			foreach (var mxRecord in dnsResponse.Answers.MxRecords().OrderBy(mx => mx.Preference))
			{
				if (await TrySendToMx(mxRecord.Exchange, dns, mails, token))
				{
					return;
				}
			}
		}

		private static async Task<bool> TrySendToMx(DnsString target, LookupClient dns, List<IMailReference> mails, CancellationToken token)
		{
			var aRecord = await dns.QueryAsync(target, QueryType.A, token);
			IPAddress targetIp = aRecord.Answers.ARecords().FirstOrDefault()?.Address;
			if (targetIp == null)
			{
				var aaaaRecord = await dns.QueryAsync(target, QueryType.AAAA, token);
				targetIp = aaaaRecord.Answers.AaaaRecords().FirstOrDefault()?.Address;
			}

			if (targetIp == null)
				return false;

			using (TcpClient mxClient = new TcpClient())
			{
				await mxClient.ConnectAsync(targetIp, 25);
			}
		}
	}
}
