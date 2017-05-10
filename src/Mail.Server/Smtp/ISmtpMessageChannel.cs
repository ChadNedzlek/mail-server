using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp
{
	public interface ISmtpMessageChannel
	{
		UserData AuthenticatedUser { get; set; }
		string ConnectedHost { get; set; }
		bool IsAuthenticated { get; }
		Task SendReplyAsync(SmtpReplyCode smtpReplyCode, bool more, string message, CancellationToken token);
		Task SendReplyAsync(SmtpReplyCode smtpReplyCode, IEnumerable<string> messages, CancellationToken cancellationToken);
		void Close();
	}

	public static class SmtpMessageChannel
	{
		public static Task SendReplyAsync(
			this ISmtpMessageChannel channel,
			SmtpReplyCode smtpReplyCode,
			string message,
			CancellationToken token)
		{
			return channel.SendReplyAsync(smtpReplyCode, false, message, token);
		}

		public static Task SendReplyAsync(
			this ISmtpMessageChannel channel,
			SmtpReplyCode smtpReplyCode,
			CancellationToken cancellationToken)
		{
			return channel.SendReplyAsync(smtpReplyCode, false, null, cancellationToken);
		}
	}
}
