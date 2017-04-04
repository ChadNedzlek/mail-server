using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Mail.Dispatcher.Test
{
    public class MockMailQueue : IMailQueue
    {
        public class MockReference : IMailReference, IMailReadReference, IMailWriteReference
        {
            public MockReference(string id, string sender, IImmutableList<string> recipients, bool saved)
                : this(id, sender, recipients, saved, (byte[]) null)
            {
            }

            public MockReference(string id, string sender, IImmutableList<string> recipients, bool saved, byte[] body)
            {
                Id = id;
                Sender = sender;
                Recipients = recipients;
                BodyStream = body == null ? new MemoryStream() : new MemoryStream(body);
                IsSaved = saved;
            }

            public MockReference(string id, string sender, IImmutableList<string> recipients, bool saved, string body)
                : this(id, sender, recipients, saved, Encoding.ASCII.GetBytes(body))
            {
            }

            public string Id { get; }
            public string Sender { get; }
            public IImmutableList<string> Recipients { get; }
            public bool IsSaved { get; private set; }

            public void Dispose()
            {
                BodyStream?.Dispose();
            }

            public Task SaveAsync(CancellationToken token)
            {
                IsSaved = true;
                return Task.CompletedTask;
            }

            public Stream BodyStream { get; }
        }

        public readonly IList<MockReference> References = new List<MockReference>();
        public readonly IList<MockReference> DeletedReferences = new List<MockReference>();
        public int Count => References.Count + DeletedReferences.Count;

        public Task<IMailWriteReference> NewMailAsync(string sender, IImmutableList<string> recipients, CancellationToken token)
        {
            var reference = new MockReference($"mail-{Count}", sender, recipients, false);
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
            MockReference mockReference = (MockReference) reference;
            References.Remove(mockReference);
            DeletedReferences.Add(mockReference);
            return Task.CompletedTask;
        }
    }
}