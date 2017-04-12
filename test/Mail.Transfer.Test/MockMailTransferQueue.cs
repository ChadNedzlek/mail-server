using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Test.Utilities;

namespace Mail.Transfer.Test
{
	internal class MockMailTransferQueue : IMailTransferQueue
	{
		public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();
		public readonly IList<MockMailReference> References = new List<MockMailReference>();
		public int Count => References.Count + DeletedReferences.Count;

		public Task<IMailWriteReference> NewMailAsync(string id, string sender, IImmutableList<string> recipients, CancellationToken token)
		{
			var reference = new MockMailReference(id, sender, recipients, false, this);
			References.Add(reference);
			return Task.FromResult((IMailWriteReference)reference);
		}

		public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
		{
			return Task.FromResult((IMailReadReference)reference);
		}

		public Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			((MockMailReference)reference).IsSaved = true;
			return Task.CompletedTask;
		}

		public Task DeleteAsync(IMailReference reference)
		{
			var mockReference = (MockMailReference)reference;
			References.Remove(mockReference);
			DeletedReferences.Add(mockReference);
			return Task.CompletedTask;
		}

		public IEnumerable<string> GetAllPendingDomains()
		{
			return References.Select(r => MailUtilities.GetDomainFromMailbox(r.Recipients[0])).Distinct();
		}

		public IEnumerable<IMailReference> GetAllMailForDomain(string domain)
		{
			return References.Where(
				r => String.Equals(
					MailUtilities.GetDomainFromMailbox(r.Recipients[0]),
					domain,
					StringComparison.OrdinalIgnoreCase));
		}
	}
}