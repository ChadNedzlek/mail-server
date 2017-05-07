﻿using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("QUIT")]
	public class QuitCommand : BaseSmtpCommand
	{
		private readonly IMessageChannel _channel;

		public QuitCommand(IMessageChannel channel)
		{
			_channel = channel;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			await _channel.SendReplyAsync(ReplyCode.Closing, "Ok", token);
			_channel.Close();
		}
	}
}
