using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace Mail.Dispatcher
{
	public class MailDispatcher
	{
		public DispatcherSettings Settings { get; }
		public IMailStore PendingMailStore { get; set; }
		public IDifferentiatedMailStore DeliveryMailStore { get; set; }

		private readonly Mutex _deliveryQueueMutex;

		private MutexThread _mutexThread;

		public MailDispatcher(DispatcherSettings settings)
		{
			Settings = settings;
			if (!Mutex.TryOpenExisting(Settings.IncomingMutexName, out _deliveryQueueMutex))
			{
				_deliveryQueueMutex = new Mutex(false, Settings.IncomingMutexName);
			}
		}

		public async Task Start(CancellationToken token)
		{
			_mutexThread = MutexThread.Begin(token);
			while (!token.IsCancellationRequested)
			{
				foreach (var reference in PendingMailStore.GetAllMailReferences().ToList())
				{
					IMailReadReference readReference;

					try
					{
						readReference = await PendingMailStore.OpenReadAsync(reference);
					}
					catch(IOException)
					{
						// It's probably a sharing violation, just try again later.
						continue;
					}

					using (readReference)
					using (var bodyStream = readReference.BodyStream)
					{
						var headers = await MailUtilities.ParseHeadersAsync(bodyStream);
						bodyStream.Seek(0, SeekOrigin.Begin);
						var dispatchReferenecs = await CreateDispatchesAsync(readReference, headers);
						using (var targetStream = new MultiStream(dispatchReferenecs.Select(r => r.BodyStream)))
						{
							await bodyStream.CopyToAsync(targetStream);
						}
					}

					await PendingMailStore.DeleteAsync(reference);
				}
			}
		}

		private Task<IMailWriteReference[]> CreateDispatchesAsync(IMailReadReference readReference, IDictionary<string, IEnumerable<string>> headers)
		{
			var recipients = AugmentRecipients(readReference.Recipients, headers);
			return Task.WhenAll(recipients
				.GroupBy(GetDomain)
				.Select(g => DeliveryMailStore.NewMailAsync(g.Key, readReference.Sender, g, CancellationToken.None)));
		}

		private IEnumerable<string> AugmentRecipients(
			IImmutableList<string> originalRecipients,
			IDictionary<string, IEnumerable<string>> headers)
		{
			List<string> existingToHeaders = new List<string>();
			IEnumerable<string> targets;
			if (headers.TryGetValue("To", out targets))
			{
				existingToHeaders.AddRange(ParseMailboxListHeader(targets));
			}
			if (headers.TryGetValue("Cc", out targets))
			{
				existingToHeaders.AddRange(ParseMailboxListHeader(targets));
			}

			IEnumerable<string> expandedRecipients = ExpandDistributionLists(originalRecipients, existingToHeaders);

			throw new NotImplementedException();
		}

		private IEnumerable<string> ExpandDistributionLists(IImmutableList<string> originalRecipients, List<string> exclude)
		{
			throw new NotImplementedException();
		}

		private IEnumerable<string> ParseMailboxListHeader(IEnumerable<string> header)
		{
			throw new NotImplementedException();
		}

		private string GetDomain(string address)
		{
			return address.Split('@').Last();
		}
	}

	public interface IDifferentiatedMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string domain, string sender, IEnumerable<string> recipients, CancellationToken token);

		IEnumerable<IMailReference> GetAllMailReferences(string domain);
		Task<IMailReadReference> OpenReadAsync(IMailReference reference);

		Task DeleteAsync(IMailReference reference);
	}
}