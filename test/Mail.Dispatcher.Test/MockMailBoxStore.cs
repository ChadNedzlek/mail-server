using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Dispatcher;
using Vaettir.Mail.Server;

namespace Mail.Dispatcher.Test
{
    public class MockMailBoxStore : IMailBoxStore
    {
        public readonly IList<MockMailReference> References = new List<MockMailReference>();
        public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();

        public int Count => References.Count + DeletedReferences.Count;
		public IList<MockMailReference> SavedReferences => References.Where(r => r.IsSaved).ToList();

		public Task<IMailWriteReference> NewMailAsync(string mailbox, CancellationToken token)
        {
            var reference = new MockMailReference($"tranfser-{Count}", "ignored", ImmutableList.Create(mailbox), false);
			References.Add(reference);
			return Task.FromResult((IMailWriteReference)reference);
        }
    }
}