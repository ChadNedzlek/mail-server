using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vaettir.Mail.Server
{
	public interface IMailQueue : IMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string sender, IImmutableList<string> recipients, CancellationToken token);
		IEnumerable<IMailReference> GetAllMailReferences();
		Stream GetTemporaryMailStream([NotNull] IMailReadReference reference);
	}
}
