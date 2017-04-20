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

		public readonly Dictionary<string, Dictionary<string, List<MockMailboxItemReference>>> References =
			new Dictionary<string, Dictionary<string, List<MockMailboxItemReference>>>();

		public int Count => References.Count + DeletedReferences.Count;

		public IEnumerable<MockMailboxItemReference> SavedReferences => References.Values
			.SelectMany(v => v.Values)
			.SelectMany(v => v)
			.Where(r => r.IsSaved);

		public Task<IMailboxItemWriteReference> NewMailAsync(
			string id,
			string mailbox,
			string folder,
			CancellationToken token)
		{
			var reference = new MockMailboxItemReference(id, mailbox, folder, MailboxFlags.None, false, this);
			GetFolderItems(reference).Add(reference);
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
			GetFolderItems(mockRef).Remove(mockRef);
			DeletedReferences.Add(mockRef);
			return Task.CompletedTask;
		}

		public Task<IEnumerable<IMailboxItemReference>> GetMails(string mailbox, string folder, CancellationToken token)
		{
			return Task.FromResult((IEnumerable<IMailboxItemReference>) GetFolderItems(mailbox, folder));
		}

		public Task<IEnumerable<string>> GetFolders(string mailbox, string folder, CancellationToken token)
		{
			return Task.FromResult(GetMailbox(mailbox).Where(f => f.Key.StartsWith(folder + "/")).Select(f => f.Key));
		}

		public Task MoveAsync(IMailboxItemReference reference, string folder, CancellationToken token)
		{
			var mockRef = (MockMailboxItemReference) reference;
			GetFolderItems(mockRef).Remove(mockRef);
			mockRef.Folder = folder;
			GetFolderItems(mockRef).Add(mockRef);
			return Task.CompletedTask;
		}

		public Task SetFlags(IMailboxItemReference reference, MailboxFlags flags, CancellationToken token)
		{
			((MockMailboxItemReference) reference).Flags = flags;
			return Task.CompletedTask;
		}

		private ICollection<MockMailboxItemReference> GetFolderItems(MockMailboxItemReference mockRef)
		{
			return GetFolderItems(mockRef.Mailbox, mockRef.Folder);
		}

		private ICollection<MockMailboxItemReference> GetFolderItems(string mailbox, string folder)
		{
			Dictionary<string, List<MockMailboxItemReference>> folders = GetMailbox(mailbox);

			if (!folders.TryGetValue(folder, out var collection))
			{
				folders.Add(folder, collection = new List<MockMailboxItemReference>());
			}

			return collection;
		}

		private Dictionary<string, List<MockMailboxItemReference>> GetMailbox(string mailbox)
		{
			if (!References.TryGetValue(mailbox, out var folders))
			{
				References.Add(mailbox, folders = new Dictionary<string, List<MockMailboxItemReference>>());
			}

			return folders;
		}
	}
}
