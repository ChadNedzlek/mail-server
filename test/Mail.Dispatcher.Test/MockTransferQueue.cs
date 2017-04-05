using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Dispatcher;
using Vaettir.Mail.Server;

namespace Mail.Dispatcher.Test
{
    public class MockTransferQueue : IMailTransferQueue
	{
		public readonly IList<MockMailReference> References = new List<MockMailReference>();
		public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();

		public int Count => References.Count + DeletedReferences.Count;
		public IList<MockMailReference> SavedReferences => References.Where(r => r.IsSaved).ToList();

		public Task<IMailWriteReference> NewMailAsync(IImmutableList<string> recipients, string sender, CancellationToken token)
		{
		    var reference = new MockMailReference($"tranfser-{Count}", sender, recipients, false);
		    References.Add(reference);
		    return Task.FromResult((IMailWriteReference) reference);
		}

	    public Task<IMailWriteReference> NewMailAsync(string sender, IImmutableList<string> recipients, CancellationToken token)
	    {
	        throw new System.NotImplementedException();
	    }

	    public IEnumerable<IMailReference> GetAllMailReferences()
	    {
	        throw new System.NotImplementedException();
	    }

	    public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
	    {
	        throw new System.NotImplementedException();
	    }

	    public Task DeleteAsync(IMailReference reference)
	    {
	        throw new System.NotImplementedException();
	    }
	}
}