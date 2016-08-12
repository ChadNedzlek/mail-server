using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public interface ICommandFactory : IFactory
	{
		ICommand CreateCommand(string arguments);
	}

	public interface ICommand
	{
		Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token);
	}
}