using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

		public AppendCommand(IImapMailStore mailstore, IImapMessageChannel channel)
		{
			_mailstore = mailstore;
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			switch (arguments.Count)
			{
				case 4:
				{
					IMessageData second = arguments[1];
					if (!(second is ListMessageData secondList))
					{
						return false;
					}

					_flags = secondList;
					if (!MessageData.TryGetDateTime(arguments[2], Encoding.ASCII, out DateTime localDate))
					{
						return false;
					}

					_date = localDate;
					goto case 2;
				}
				case 3:
				{
					IMessageData second = arguments[1];
					if (second is ListMessageData secondList)
					{
						_flags = secondList;
					}
					else
					{
						if (!MessageData.TryGetDateTime(second, Encoding.ASCII, out DateTime localDate))
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

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return false;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			using (Stream writeStream = await _mailstore.OpenBinaryAsync(
				_mailbox,
				_date ?? DateTime.UtcNow,
				_flags?.Items.Select(f => MessageData.GetString(f, Encoding.ASCII)),
				cancellationToken))
			{
				using (var readStream = await _channel.ReadLiteralDataAsync(cancellationToken))
				{
					int toRead = _messageBody.Length;
					byte[] buffer = null;
					try
					{
						buffer = ArrayPool<byte>.Shared.Rent(4096);
						int read;
						while ((read = await readStream.ReadBytesAsync(
							buffer,
							0,
							Math.Min(toRead, buffer.Length),
							cancellationToken)) != 0)
						{
							await writeStream.WriteAsync(buffer, 0, read, cancellationToken);
						}
					}
					finally
					{
						if (buffer != null)
						{
							ArrayPool<byte>.Shared.Return(buffer);
						}
					}
				}
			}

			await EndOkAsync(_channel, cancellationToken);
		}
	}
}
