using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockMailReference : IMailReference, IMailReadReference, IMailWriteReference
	{
		public MockMailReference(string id, string sender, IImmutableList<string> recipients, bool saved, IMailStore store)
			: this(id, sender, recipients, saved, (byte[]) null, store)
		{
		}

		public MockMailReference(
			string id,
			string sender,
			IImmutableList<string> recipients,
			bool saved,
			byte[] body,
			IMailStore store)
		{
			Id = id;
			Sender = sender;
			Recipients = recipients;
			BackupBodyStream = body == null ? new MemoryStream() : new MemoryStream(body);
			BodyStream = new MultiStream(new[] {BackupBodyStream}, true);
			IsSaved = saved;
			Store = store;
		}

		public MockMailReference(
			string id,
			string sender,
			IImmutableList<string> recipients,
			bool saved,
			string body,
			IMailStore store)
			: this(id, sender, recipients, saved, Encoding.ASCII.GetBytes(body), store)
		{
		}

		public bool IsSaved { get; set; }

		public MemoryStream BackupBodyStream { get; }
		public string Sender { get; }
		public IImmutableList<string> Recipients { get; }
		public IMailStore Store { get; }

		public void Dispose()
		{
			BodyStream?.Dispose();
			BackupBodyStream?.Dispose();
		}

		public Stream BodyStream { get; }

		public string Id { get; }
		IWriter IWritable.Store => Store;

		public Task SaveAsync(CancellationToken token)
		{
			IsSaved = true;
			return Task.CompletedTask;
		}
	}
}
