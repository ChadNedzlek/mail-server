using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Commands;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap
{
	public interface IImapMessageChannel
	{
		Task CommandCompletedAsync(Message message, IImapCommand command, CancellationToken cancellationToken);
		Task SendMessageAsync(Message message, Encoding encoding, CancellationToken cancellationToken);
		UserData AuthenticatedUser { get; set; }
		SessionState State { get; set; }
		void EndSession();
		Task EndCommandWithoutResponseAsync(IImapCommand command, CancellationToken cancellationToken);
		void DiscardPendingExpungeResponses();
	}

	public static class ImapMessageChannel
	{
		private static Encoding DefaultEncoding { get; } = Encoding.ASCII;

		public static Task SendMessageAsync(this IImapMessageChannel channel, Message message, CancellationToken cancellationToken)
		{
			return channel.SendMessageAsync(message, DefaultEncoding, cancellationToken);
		}

		public static Task ReportBadAsync(IImapMessageChannel imapSession, string tag, string errorText, CancellationToken cancellationToken)
		{
			var message = new Message(tag, "BAD", new List<IMessageData> {new ServerMessageData(errorText)});
			return imapSession.SendMessageAsync(message, DefaultEncoding, cancellationToken);
		}

		public static Task SendContinuationAsync(ImapSession imapSession, string text, CancellationToken cancellationToken)
		{
			return imapSession.SendContinuationAsync(text, DefaultEncoding, cancellationToken);
		}
	}
}