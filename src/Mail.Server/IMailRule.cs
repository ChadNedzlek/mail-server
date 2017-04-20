using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailRule
	{
		Task RunAsync(IMailReference referece, CancellationToken token);
	}
}
