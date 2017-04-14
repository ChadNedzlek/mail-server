using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockMailboxStore : IMailboxStore
	{
		public readonly IList<MockMailboxItemReference> DeletedReferences = new List<MockMailboxItemReference>();

		public readonly Dictionary<string, List<MockMailboxItemReference>> References =
			new Dictionary<string, List<MockMailboxItemReference>>();

		public int Count => References.Count + DeletedReferences.Count;

		public IEnumerable<MockMailboxItemReference> SavedReferences => References.Values.SelectMany(v => v)
			.Where(r => r.IsSaved);

		public Task<IMailboxItemWriteReference> NewMailAsync(
			string id,
			string mailbox,
			string folder,
			CancellationToken token)
		{
			var reference = new MockMailboxItemReference(id, mailbox, folder, MailboxFlags.None, false, this);
			AddToFolder(folder, reference);
			return Task.FromResult((IMailboxItemWriteReference) reference);
		}

		public Task<IMailboxItemReadReference> OpenReadAsync(IMailboxItemReference reference, CancellationToken token)
		{
			return Task.FromResult((IMailboxItemReadReference) reference);
		}

		public Task SaveAsync(IWritable reference, CancellationToken token)
		{
			var mockRef = (MockMailboxItemReference) reference;
			mockRef.IsSaved = true;
			return Task.CompletedTask;
		}

		public Task DeleteAsync(IMailboxItemReference reference)
		{
			var mockRef = (MockMailboxItemReference) reference;
			References.FirstOrDefault(p => p.Value.Contains(mockRef)).Value.Remove(mockRef);
			DeletedReferences.Add(mockRef);
			return Task.CompletedTask;
		}

		public Task MoveAsync(IMailboxItemReference reference, string folder, CancellationToken token)
		{
			var mockRef = (MockMailboxItemReference) reference;
			References.FirstOrDefault(p => p.Value.Contains(mockRef)).Value.Remove(mockRef);
			AddToFolder(folder, mockRef);
			return Task.CompletedTask;
		}

		public Task SetFlags(IMailboxItemReference reference, MailboxFlags flags, CancellationToken token)
		{
			((MockMailboxItemReference) reference).Flags = flags;
			return Task.CompletedTask;
		}

		public void AddToFolder(string folder, MockMailboxItemReference reference)
		{
			if (!References.TryGetValue(folder, out var folderList))
			{
				References.Add(folder, folderList = new List<MockMailboxItemReference>());
			}
			folderList.Add(reference);
		}
	}
}
