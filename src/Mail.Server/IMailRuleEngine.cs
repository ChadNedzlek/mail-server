using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IMailRuleEngine
	{
		Task RunAllRules(IMailReference reference, CancellationToken token);
	}
}
