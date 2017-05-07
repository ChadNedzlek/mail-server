using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp
{
	public interface ISmtpCommand
	{
		Task ExecuteAsync(CancellationToken token);
		void Initialize(string command);
	}
}
