using System.Collections.Generic;
using System.IO;
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
		UserData AuthenticatedUser { get; set; }
		SessionState State { get; set; }
		Task CommandCompletedAsync(ImapMessage message, IImapCommand command, CancellationToken cancellationToken);
		Task SendMessageAsync(ImapMessage message, Encoding encoding, CancellationToken cancellationToken);
		void EndSession();
		Task EndCommandWithoutResponseAsync(IImapCommand command, CancellationToken cancellationToken);
		void DiscardPendingExpungeResponses();
		void SetAuthenticatedUser(UserData userData);
		Task<IVariableStreamReader> ReadLiteralDataAsync(CancellationToken cancellationToken);
	}

	public static class ImapMessageChannel
	{
		private static Encoding DefaultEncoding { get; } = Encoding.ASCII;

		public static Task SendMessageAsync(
			this IImapMessageChannel channel,
			ImapMessage message,
			CancellationToken cancellationToken)
		{
			return channel.SendMessageAsync(message, DefaultEncoding, cancellationToken);
		}

		public static Task ReportBadAsync(
			IImapMessageChannel imapSession,
			string tag,
			string errorText,
			CancellationToken cancellationToken)
		{
			var message = new ImapMessage(tag, "BAD", new List<IMessageData> {new ServerMessageData(errorText)});
			return imapSession.SendMessageAsync(message, DefaultEncoding, cancellationToken);
		}

		public static Task SendContinuationAsync(ImapSession imapSession, string text, CancellationToken cancellationToken)
		{
			return imapSession.SendContinuationAsync(text, DefaultEncoding, cancellationToken);
		}
	}
}
