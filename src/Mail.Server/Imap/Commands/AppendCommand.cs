using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("APPEND", SessionState.Authenticated)]
	public class AppendCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;
		private readonly IImapMailStore _mailstore;
		private DateTime? _date;
		private ListMessageData _flags;
		private string _mailbox;
		private LiteralMessageData _messageBody;

		public AppendCommand(IImapMailStore mailstore)
		{
			_mailstore = mailstore;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			switch (arguments.Count)
			{
				case 4:
				{
					IMessageData second = arguments[1];
					var secondList = second as ListMessageData;
					if (secondList == null)
					{
						return false;
					}

					_flags = secondList;
					DateTime localDate;
					if (!MessageData.TryGetDateTime(arguments[2], Encoding.ASCII, out localDate))
					{
						return false;
					}

					_date = localDate;
					goto case 2;
				}
				case 3:
				{
					IMessageData second = arguments[1];
					var secondList = second as ListMessageData;
					if (secondList != null)
					{
						_flags = secondList;
					}
					else
					{
						DateTime localDate;
						if (!MessageData.TryGetDateTime(second, Encoding.ASCII, out localDate))
						{
							return false;
						}

						_date = localDate;
					}

					goto case 2;
				}
				case 2:
					_mailbox = MessageData.GetString(arguments[0], Encoding.UTF8);
					_messageBody = arguments[arguments.Count - 1] as LiteralMessageData;
					return true;
				default:
					return false;
			}
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			await _mailstore.SaveBinaryAsync(
				_mailbox,
				_date ?? DateTime.UtcNow,
				_flags?.Items.Select(f => MessageData.GetString(f, Encoding.ASCII)),
				_messageBody.Data,
				cancellationToken);

			await EndOkAsync(_channel, cancellationToken);
		}
	}
}
