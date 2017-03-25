using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp
{
	public interface ICommand
	{
		Task ExecuteAsync(CancellationToken token);
	    void Initialize(string command);
	}
}