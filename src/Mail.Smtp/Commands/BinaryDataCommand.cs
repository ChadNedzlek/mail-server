using System;
using System.Threading;
using System.Threading.Tasks;
using MailServer;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("BDAT")]
    public class BinaryDataCommand : BaseCommand
    {
        private readonly SecurableConnection _connection;
        private readonly SmtpSession _session;
        private readonly IMailStore _mailStore;
        private readonly IMailBuilder _builder;

        public BinaryDataCommand(
			SecurableConnection connection,
			SmtpSession session,
			IMailStore mailStore,
			IMailBuilder builder)
        {
            _connection = connection;
            _session = session;
            _mailStore = mailStore;
            _builder = builder;
        }

        public override async Task ExecuteAsync(CancellationToken token)
        {
            if (String.IsNullOrEmpty(_builder.PendingMail?.FromPath?.Mailbox) ||
				_builder.PendingMail?.Recipents?.Count == 0 ||
				_builder.PendingMail?.IsBinary != true)
            {
                await _session.SendReplyAsync(ReplyCode.BadSequence, "Bad sequence", token);
                return;
            }

            string[] parts = Arguments?.Split(' ');
            if (parts == null || parts.Length == 0 || parts.Length > 2)
            {
                await _session.SendReplyAsync(ReplyCode.InvalidArguments, "Length required, optional LAST", token);
                return;
            }

            int length;
            if (!Int32.TryParse(parts[0], out length) || length < 1)
            {
                await _session.SendReplyAsync(ReplyCode.InvalidArguments, "Length must be positive integer", token);
                return;
            }

            bool last = false;
            if (parts.Length == 2)
            {
                if (!String.Equals("LAST", parts[1]))
                {
                    await _session.SendReplyAsync(ReplyCode.InvalidArguments, "LAST expected", token);
                    return;
                }
                last = true;
            }

            using (
                var mailReference = await _mailStore.NewMailAsync(
					_builder.PendingMail.FromPath.Mailbox,
					_builder.PendingMail.Recipents,
                    token))
            {
                using (var mailStream = mailReference.BodyStream)
                {

                    byte[] chunk = new byte[1000];
                    int totalRead = 0;
                    do
                    {
                        int toRead = Math.Min(chunk.Length, length - totalRead);
                        int read = await _connection.ReadBytesAsync(chunk, 0, toRead, token);
                        totalRead += read;
                        await mailStream.WriteAsync(chunk, 0, read, token);
                    } while (totalRead < length);
                }

                await mailReference.SaveAsync(token);
            }

            await _session.SendReplyAsync(ReplyCode.Okay, $"Recieved {length} octets", token);

            if (last)
            {
				_builder.PendingMail = null;
                await _session.SendReplyAsync(ReplyCode.Okay, "Message complete", token);
            }
        }
    }
}