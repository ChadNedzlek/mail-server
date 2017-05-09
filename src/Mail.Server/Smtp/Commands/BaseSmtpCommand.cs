using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	public abstract class BaseSmtpCommand : ISmtpCommand
	{
		protected string Arguments { get; private set; }

		public abstract Task ExecuteAsync(CancellationToken token);

		public void Initialize(string command)
		{
			Arguments = command;
		}

		protected virtual bool TryProcessParameter(string key, string value)
		{
			return false;
		}

		protected bool TryProcessParameterValue(
			ISmtpMessageChannel channel,
			string parameterString,
			out Task errorReport,
			CancellationToken cancellationToken)
		{
			foreach (string parameter in parameterString.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				int sepIndex = parameter.IndexOf("=", StringComparison.Ordinal);
				if (sepIndex == -1)
				{
					errorReport = channel.SendReplyAsync(ReplyCode.InvalidArguments, "Bad parameters", cancellationToken);
					return false;
				}

				string paramKey = parameter.Substring(0, sepIndex);
				string paramValue = parameter.Substring(sepIndex + 1);
				if (!TryProcessParameter(paramKey, paramValue))
				{
					errorReport = channel.SendReplyAsync(
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
