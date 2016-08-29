using System.Collections.Generic;
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
						var dispatchReferenecs = await CreateDispatchesAsync(readReference);
						using (var targetStream = new MultiStream(dispatchReferenecs.Select(r => r.BodyStream)))
						{
							await bodyStream.CopyToAsync(targetStream);
						}
					}

					await PendingMailStore.DeleteAsync(reference);
				}
			}
		}

		private Task<IMailWriteReference[]> CreateDispatchesAsync(IMailReadReference readReference)
		{
			return Task.WhenAll(readReference.Recipients
				.GroupBy(GetDomain)
				.Select(g => DeliveryMailStore.NewMailAsync(g.Key, readReference.Sender, g, CancellationToken.None)));
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