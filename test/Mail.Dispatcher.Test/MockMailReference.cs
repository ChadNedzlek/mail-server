using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace Mail.Dispatcher.Test
{
    public class MockMailReference : IMailReference, IMailReadReference, IMailWriteReference
    {
        public MockMailReference(string id, string sender, IImmutableList<string> recipients, bool saved)
            : this(id, sender, recipients, saved, (byte[])null)
        {
        }

        public MockMailReference(string id, string sender, IImmutableList<string> recipients, bool saved, byte[] body)
        {
            Id = id;
            Sender = sender;
            Recipients = recipients;
            BackupBodyStream = body == null ? new MemoryStream() : new MemoryStream(body);
            BodyStream = new MultiStream(new[] {BackupBodyStream}, true);
            IsSaved = saved;
        }

        public MockMailReference(string id, string sender, IImmutableList<string> recipients, bool saved, string body)
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
            BackupBodyStream?.Dispose();
        }

        public Task SaveAsync(CancellationToken token)
        {
            IsSaved = true;
            return Task.CompletedTask;
        }

        public Stream BodyStream { get; }

        public Stream BackupBodyStream { get; }
    }
}