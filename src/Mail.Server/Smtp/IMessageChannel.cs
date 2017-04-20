using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp
{
	public interface IMessageChannel
	{
		UserData AuthenticatedUser { get; set; }
		string ConnectedHost { get; set; }
		bool IsAuthenticated { get; }
		Task SendReplyAsync(ReplyCode replyCode, bool more, string message, CancellationToken token);
		Task SendReplyAsync(ReplyCode replyCode, IEnumerable<string> messages, CancellationToken cancellationToken);
		void Close();
	}

	public static class MessageChannel
	{
		public static Task SendReplyAsync(
			this IMessageChannel channel,
			ReplyCode replyCode,
			string message,
			CancellationToken token)
		{
			return channel.SendReplyAsync(replyCode, false, message, token);
		}

		public static Task SendReplyAsync(
			this IMessageChannel channel,
			ReplyCode replyCode,
			CancellationToken cancellationToken)
		{
			return channel.SendReplyAsync(replyCode, false, null, cancellationToken);
		}
	}
}
