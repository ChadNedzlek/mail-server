using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	public abstract class SubscribeOrUnsubscribeCommand : BaseImapCommand
	{
		private string _mailbox;
		private readonly IImapMessageChannel _channel;
		private readonly IImapMailStore _mailstore;

		protected SubscribeOrUnsubscribeCommand(IImapMessageChannel channel, IImapMailStore mailstore)
		{
			_channel = channel;
			_mailstore = mailstore;
		}

		public abstract bool IsSubscribe { get; }

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 1) return false;

			_mailbox = MessageData.GetString(arguments[0], Encoding.UTF8);

			if (string.IsNullOrEmpty(_mailbox)) return false;

			return true;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			try
			{
				await
					_mailstore.MarkMailboxSubscribedAsync(_channel.AuthenticatedUser, _mailbox, IsSubscribe, cancellationToken);
			}
			catch (Exception)
			{
				await EndWithResultAsync(_channel, CommandResult.No, "cannot subsribe to mailbox", cancellationToken);
				return;
			}

			await EndOkAsync(_channel, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}
	}
}