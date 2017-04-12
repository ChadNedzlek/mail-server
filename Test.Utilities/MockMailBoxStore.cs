using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Test.Utilities
{
    public class MockMailBoxStore : IMailBoxStore
    {
        public readonly Dictionary<string, List<MockMailReference>> References = new Dictionary<string, List<MockMailReference>>();
        public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();

        public int Count => References.Count + DeletedReferences.Count;

	    public IEnumerable<MockMailReference> SavedReferences => References.Values.SelectMany(v => v)
		    .Where(r => r.IsSaved);

	    public Task<IMailWriteReference> NewMailAsync(string mailbox, string folder, CancellationToken token)
	    {
			var reference = new MockMailReference($"tranfser-{Count}", "ignored", ImmutableList.Create(mailbox), false, this);
		    AddToFolder(folder, reference);
		    return Task.FromResult((IMailWriteReference)reference);
        }

	    public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
	    {
		    throw new System.NotImplementedException();
	    }

	    public Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			var mockRef = (MockMailReference)reference;
			mockRef.IsSaved = true;
			return Task.CompletedTask;
		}

	    public Task DeleteAsync(IMailReference reference)
	    {
		    var mockRef = (MockMailReference) reference;
		    References.FirstOrDefault(p => p.Value.Contains(mockRef)).Value.Remove(mockRef);
		    DeletedReferences.Add(mockRef);
		    return Task.CompletedTask;
		}

	    public Task MoveAsync(IMailReference reference, string folder, CancellationToken token)
		{
			var mockRef = (MockMailReference)reference;
			References.FirstOrDefault(p => p.Value.Contains(mockRef)).Value.Remove(mockRef);
			AddToFolder(folder, mockRef);
			return Task.CompletedTask;
		}

	    public Task SetFlags(IMailReference reference, IEnumerable<string> flags, CancellationToken token)
	    {
		    throw new System.NotImplementedException();
	    }
		
	    public void AddToFolder(string folder, MockMailReference reference)
	    {
		    if (!References.TryGetValue(folder, out var folderList))
		    {
			    References.Add(folder, folderList = new List<MockMailReference>());
		    }
		    folderList.Add(reference);
	    }
	}
}