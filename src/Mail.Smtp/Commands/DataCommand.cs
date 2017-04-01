using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
    [Command("DATA")]
	public class DataCommand : BaseCommand
	{
		private readonly IMailQueue _mailQueue;
		private readonly SmtpSettings _settings;
		private readonly SecurableConnection _connection;
		private readonly ConnectionInformation _connectionInformation;
	    private readonly IMailBuilder _builder;
	    private readonly IMessageChannel _channel;

	    public DataCommand(
			IMailQueue mailQueue,
			SmtpSettings settings,
			SecurableConnection connection,
			ConnectionInformation connectionInformation,
			IMailBuilder builder,
			IMessageChannel channel
			) 
		{
			_mailQueue = mailQueue;
			_settings = settings;
			_connection = connection;
			_connectionInformation = connectionInformation;
		    _builder = builder;
		    _channel = channel;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			if (String.IsNullOrEmpty(_builder.PendingMail?.FromPath?.Mailbox) ||
				_builder.PendingMail?.Recipents?.Count == 0 ||
				_builder.PendingMail?.IsBinary == true)
			{
				await _channel.SendReplyAsync(ReplyCode.BadSequence, "Bad sequence", token);
				return;
			}

			await _channel.SendReplyAsync(ReplyCode.StartMail, "Send data, end with .<CR><LF>", token);

		    using (IMailWriteReference reference = await _mailQueue.NewMailAsync(
		        _builder.PendingMail.FromPath.Mailbox,
		        _builder.PendingMail.Recipents,
		        token))
		    {
		        using (var mailWriter = new StreamWriter(reference.BodyStream, Encoding.UTF8))
		        {
		            await
		                mailWriter.WriteLineAsync(
		                    $"Received: FROM {_channel.ConnectedHost} ({_connectionInformation.RemoteAddress}) BY {_settings.DomainName} ({_connectionInformation.LocalAddress}); {DateTime.UtcNow:ddd, dd MMM yyy HH:mm:ss zzzz}");

		            string line;
		            while ((line = await _connection.ReadLineAsync(Encoding.UTF8, token)) != ".")
		            {
		                await mailWriter.WriteLineAsync(line);
		            }
		        }

		        await reference.SaveAsync(token);
		    }


		    _builder.PendingMail = null;
			await _channel.SendReplyAsync(ReplyCode.Okay, "OK", token);
		}
	}
}