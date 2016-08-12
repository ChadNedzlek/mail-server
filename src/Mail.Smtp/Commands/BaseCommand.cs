using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	internal abstract class BaseCommand : ICommand
	{
		public BaseCommand(string name, string arguments)
		{
			Name = name;
			Arguments = arguments;
		}

		public string Name { get; }
		public string Arguments { get; }

		public abstract Task ExecuteAsync(SmtpSession smtpSession, CancellationToken token);

		protected virtual bool TryProcessParameter(SmtpSession session, string key, string value)
		{
			return false;
		}

		protected bool TryProcessParameterValue(
			SmtpSession session,
			string parameterString,
			out Task errorReport,
			CancellationToken cancellationToken)
		{
			foreach (var parameter in parameterString.Split(new [] { ' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				int sepIndex = parameter.IndexOf("=", StringComparison.Ordinal);
				if (sepIndex == -1)
				{
					errorReport = session.SendReplyAsync(ReplyCode.InvalidArguments, "Bad parameters", cancellationToken);
					return false;
				}

				string paramKey = parameter.Substring(0, sepIndex);
				string paramValue = parameter.Substring(sepIndex + 1);
				if (!TryProcessParameter(session, paramKey, paramValue))
				{
					errorReport = session.SendReplyAsync(
						ReplyCode.ParameterNotImplemented,
						"Parameter not implemented",
						cancellationToken);
					return false;
				}
			}

			errorReport = null;
			return true;
		}
	}
}