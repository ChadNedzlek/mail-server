using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailStore
	{
		Task<Stream> GetNewMailStreamAsync(string sender, IEnumerable<string> recipients, CancellationToken token);
	}
}