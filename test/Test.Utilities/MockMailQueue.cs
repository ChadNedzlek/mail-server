using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockMailQueue : IMailQueue
	{
		public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();
		public readonly IList<MockMailReference> References = new List<MockMailReference>();
		public int Count => References.Count + DeletedReferences.Count;

		public Task<IMailWriteReference> NewMailAsync(
			string sender,
			IImmutableList<string> recipients,
			CancellationToken token)
		{
			var reference = new MockMailReference($"mail-{Count}", sender, recipients, false, this);
			References.Add(reference);
			return Task.FromResult((IMailWriteReference) reference);
		}

		public IEnumerable<IMailReference> GetAllMailReferences()
		{
			return References.Where(r => r.IsSaved);
		}

		public Stream GetTemporaryMailStream(IMailReadReference reference)
		{
			return new MemoryStream();
		}

		public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
		{
			return Task.FromResult((IMailReadReference) reference);
		}

		public Task DeleteAsync(IMailReference reference)
		{
			var mockReference = (MockMailReference) reference;
			References.Remove(mockReference);
			DeletedReferences.Add(mockReference);
			return Task.CompletedTask;
		}

		public Task SaveAsync(IWritable item, CancellationToken token)
		{
			return SaveAsync((IMailWriteReference) item, token);
		}

		public Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			((MockMailReference) reference).IsSaved = true;
			return Task.CompletedTask;
		}
	}
}
