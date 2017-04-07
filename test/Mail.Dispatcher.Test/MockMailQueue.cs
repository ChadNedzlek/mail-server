using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Mail.Dispatcher.Test
{
    public class MockMailQueue : IMailQueue
    {
        public readonly IList<MockMailReference> References = new List<MockMailReference>();
        public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();
        public int Count => References.Count + DeletedReferences.Count;

        public Task<IMailWriteReference> NewMailAsync(string sender, IImmutableList<string> recipients, CancellationToken token)
        {
            var reference = new MockMailReference($"mail-{Count}", sender, recipients, false);
            References.Add(reference);
            return Task.FromResult((IMailWriteReference)reference);
        }

        public IEnumerable<IMailReference> GetAllMailReferences()
        {
            return References.Where(r => r.IsSaved);
        }

        public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
        {
            return Task.FromResult((IMailReadReference)reference);
        }

        public Task DeleteAsync(IMailReference reference)
        {
            MockMailReference mockReference = (MockMailReference) reference;
            References.Remove(mockReference);
            DeletedReferences.Add(mockReference);
            return Task.CompletedTask;
        }
    }
}