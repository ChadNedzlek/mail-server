using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;

namespace Mail.Transfer.Test
{
	internal class MockMailTransferQueue : IMailTransferQueue
	{
		public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task DeleteAsync(IMailReference reference)
		{
			throw new System.NotImplementedException();
		}

		public Task<IMailWriteReference> NewMailAsync(string id, string sender, IImmutableList<string> recipients, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<string> GetMailsByDomain()
		{
			throw new System.NotImplementedException();
		}

		public IEnumerable<IMailReference> GetAllMailForDomain(string domain)
		{
			throw new System.NotImplementedException();
		}
	}
}