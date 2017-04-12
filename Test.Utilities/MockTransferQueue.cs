using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockTransferQueue : IMailTransferQueue
	{
		public readonly IList<MockMailReference> DeletedReferences = new List<MockMailReference>();
		public readonly IList<MockMailReference> References = new List<MockMailReference>();

		public int Count => References.Count + DeletedReferences.Count;
		public IList<MockMailReference> SavedReferences => References.Where(r => r.IsSaved).ToList();

		public Task<IMailWriteReference> NewMailAsync(
			string id,
			string sender,
			IImmutableList<string> recipients,
			CancellationToken token)
		{
			var reference = new MockMailReference($"tranfser-{Count}", sender, recipients, false, this);
			References.Add(reference);
			return Task.FromResult((IMailWriteReference) reference);
		}

		public IEnumerable<string> GetMailsByDomain()
		{
			return References.Select(r => MailUtilities.GetDomainFromMailbox(r.Recipients[0])).Distinct();
		}

		public IEnumerable<IMailReference> GetAllMailForDomain(string domain)
		{
			throw new NotImplementedException();
		}

		public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
		{
			throw new NotImplementedException();
		}

		public Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			((MockMailReference) reference).IsSaved = true;
			return Task.CompletedTask;
		}

		public Task DeleteAsync(IMailReference reference)
		{
			throw new NotImplementedException();
		}
	}
}
