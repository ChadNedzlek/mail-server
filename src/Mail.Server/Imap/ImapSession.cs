using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Autofac.Features.OwnedInstances;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Imap.Commands;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Imap
{
	public sealed class ImapSession : IDisposable, IProtocolSession, IAuthenticationTransport
	{
		private readonly IIndex<string, Lazy<Owned<IImapCommand>, IImapCommandMetadata>> _commands;
		private readonly List<IImapCommand> _outstandingCommands = new List<IImapCommand>();
		private readonly Queue<IImapCommand> _pendingCommands = new Queue<IImapCommand>();
		private readonly byte[] _readBuffer = new byte[1000000];
		private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(1);
		private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1);

		public ImapSession(
			SecurableConnection connection,
			IUserStore userStore,
			IImapMailStore mailStore,
			IIndex<string, Lazy<Owned<IImapCommand>, IImapCommandMetadata>> commands)
		{
			_commands = commands;

			Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			UserStore = userStore;
			MailStore = mailStore;
			State = SessionState.Open;
		}

		public SecurableConnection Connection { get; }
		public IUserStore UserStore { get; }
		public IImapMailStore MailStore { get; }
		public SessionState State { get; private set; }
		private Encoding DefaultEncoding { get; } = Encoding.ASCII;
		public UserData AuthenticatedUser { get; private set; }
		public SelectedMailbox SelectedMailbox { get; private set; }

		public void Dispose()
		{
			Connection.Dispose();
		}

		public void EndSession()
		{
			State = SessionState.Closed;
		}

		public async Task Start(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			while (Connection.State != SecurableConnectionState.Closed)
			{
				cancellationToken.ThrowIfCancellationRequested();
				IImapCommand command;
				try
				{
					command = await ReadCommandAsync(cancellationToken);
				}
				catch (BadImapCommandFormatException e)
				{
					await ReportBadAsync(e.Tag, e.ErrorMessage ?? "Invalid command", cancellationToken);
					continue;
				}

				if (!command.HasValidArguments)
				{
					await ReportBadAsync(command.Tag, "Invalid command", cancellationToken);
					continue;
				}

				if (_pendingCommands.Count > 0)
				{
					_pendingCommands.Enqueue(command);
					continue;
				}

				if (!CanRunImmediately(command))
				{
					_pendingCommands.Enqueue(command);
					continue;
				}

				_outstandingCommands.Add(command);

				await command.ExecuteAsync(this, cancellationToken);
			}
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		private Task ReportBadAsync(string tag, string errorText, CancellationToken cancellationToken)
		{
			var message = new Message(tag, "BAD", new List<IMessageData> {new ServerMessageData(errorText)});
			return SendMessageAsync(message, DefaultEncoding, cancellationToken);
		}

		private bool CanRunImmediately(IImapCommand command)
		{
			if (!command.IsValidWith(_outstandingCommands))
			{
				return false;
			}

			var commandEnumerable = new[] {command};
			if (!_outstandingCommands.All(c => c.IsValidWith(commandEnumerable)))
			{
				return false;
			}

			return true;
		}

		internal async Task CommandCompletedAsync(Message message, IImapCommand command, CancellationToken cancellationToken)
		{
			await SendPendingResponsesAsync(cancellationToken);
			await SendMessageAsync(message, cancellationToken);

			await EndCommandWithoutResponseAsync(command, cancellationToken);
		}

		private async Task SendPendingResponsesAsync(CancellationToken cancellationToken)
		{
		}

		internal async Task<IImapCommand> ReadCommandAsync(CancellationToken cancellationToken)
		{
			List<IMessageData> data = await ReadDataAsync(cancellationToken);

			return CreateCommand(data);
		}

		public async Task<List<IMessageData>> ReadDataAsync(CancellationToken cancellationToken)
		{
			var data = new List<IMessageData>();
			string tag = null;
			while (true)
			{
				string line = await Connection.ReadLineAsync(Encoding.UTF8, cancellationToken);
				int literalLength;
				ParseLine(ref tag, line, data, out literalLength);

				if (literalLength == -1)
				{
					break;
				}

				if (_readBuffer.Length < literalLength)
				{
					// Too much data
					throw new BadImapCommandFormatException(tag, "Command line too long");
				}

				using (await SemaphoreLock.GetLockAsync(_readSemaphore, cancellationToken))
				{
					var read = 0;
					do
					{
						await Connection.WriteLineAsync("+ Ready for literal data", Encoding.ASCII, cancellationToken);
						int newRead = await Connection.ReadBytesAsync(_readBuffer, read, literalLength, cancellationToken);
						if (newRead == 0)
						{
							throw new BadImapCommandFormatException(tag);
						}
						read += newRead;
					} while (read < literalLength);

					data.Add(new LiteralMessageData(_readBuffer, literalLength));
				}
			}
			return data;
		}

		private IImapCommand CreateCommand(IReadOnlyList<IMessageData> data)
		{
			if (data.Count == 0)
			{
				throw new BadImapCommandFormatException(null);
			}
			var tagArg = data[0] as AtomMessageData;
			if (tagArg == null)
			{
				throw new BadImapCommandFormatException(null);
			}

			if (data.Count < 2)
			{
				throw new BadImapCommandFormatException(tagArg.Value);
			}

			var commandArg = data[1] as AtomMessageData;

			if (tagArg == null || commandArg == null)
			{
				throw new BadImapCommandFormatException(tagArg.Value);
			}

			if (!_commands.TryGetValue(commandArg.Value, out var command))
			{
				throw new BadImapCommandFormatException(tagArg.Value);
			}

			if (command.Metadata.MinimumState > State)
			{
				command.Value.Dispose();
				throw new BadImapCommandFormatException(tagArg.Value);
			}

			return command.Value.Value;
		}

		public Task SendContinuationAsync(string text, CancellationToken cancellationToken)
		{
			return SendContinuationAsync(text, DefaultEncoding, cancellationToken);
		}

		public async Task SendContinuationAsync(string text, Encoding encoding, CancellationToken cancellationToken)
		{
			await Connection.WriteLineAsync("+ " + text, encoding, cancellationToken);
		}

		public Task SendMessageAsync(Message message, CancellationToken cancellationToken)
		{
			return SendMessageAsync(message, DefaultEncoding, cancellationToken);
		}

		public async Task SendMessageAsync(Message message, Encoding encoding, CancellationToken cancellationToken)
		{
			using (await SemaphoreLock.GetLockAsync(_sendSemaphore, cancellationToken))
			{
				var builder = new StringBuilder();
				builder.Append(message.Tag);
				foreach (IMessageData data in message.Data)
				{
					var literal = data as LiteralMessageData;
					if (literal != null)
					{
						builder.Append("}");
						builder.Append(literal.Data.Length);
						builder.AppendLine("}");
						await Connection.WriteAsync(builder.ToString(), encoding, cancellationToken);
						builder.Clear();
					}
					else
					{
						if (builder.Length > 0)
						{
							builder.Append(" ");
						}
						builder.Append(data.ToMessageString());
					}
				}

				if (builder.Length > 0)
				{
					builder.AppendLine();
					await Connection.WriteAsync(builder.ToString(), encoding, cancellationToken);
				}
			}
		}

		internal static void ParseLine(string text, List<IMessageData> data, out int literalLength)
		{
			string tag = null;
			ParseLine(ref tag, text, data, out literalLength);
		}

		internal static void ParseLine(ref string tag, string text, List<IMessageData> data, out int literalLength)
		{
			int segmentStart = -1;
			literalLength = -1;
			var inQuote = false;
			var inLiteralLength = false;
			var inUtf7Escape = false;
			var isUtf8 = false;
			string segment = null;
			var listStack = new Stack<List<IMessageData>>();
			listStack.Push(data);

			for (var i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (inLiteralLength)
				{
					if (c == '}')
					{
						if (!int.TryParse(text.Substring(segmentStart, i - segmentStart), out literalLength))
						{
							throw new BadImapCommandFormatException(tag);
						}

						if (i != text.Length - 1)
						{
							throw new BadImapCommandFormatException(tag);
						}

						return;
					}
				}
				else if (inQuote)
				{
					switch (c)
					{
						case '\\':
						{
							char next = text[++i];
							string substring = text.Substring(segmentStart, i - segmentStart - 1);
							AddSegment(ref segment, substring);
							segmentStart = i + 1;
							AddSegment(ref segment, next.ToString());
							break;
						}
						case '"':
						{
							string substring = text.Substring(segmentStart, i - segmentStart);
							AddSegment(ref segment, substring);
							listStack.Peek().Add(new QuotedMessageData(segment));
							isUtf8 = false;
							inQuote = false;
							segmentStart = -1;
							segment = null;
							break;
						}
					}
				}
				else if (inUtf7Escape)
				{
					if (c == '-')
					{
						try
						{
							string encoded = Encoding.UTF7.GetString(
								Convert.FromBase64String(text.Substring(segmentStart, i - segmentStart)));
							AddSegment(ref segment, encoded);
						}
						catch (ArgumentException)
						{
							throw new BadImapCommandFormatException(tag);
						}
						segmentStart = i + 1;
						inUtf7Escape = false;
					}
				}
				else
				{
					if (segmentStart == -1)
					{
						switch (c)
						{
							case ' ':
								break;
							case '"':
								segmentStart = i + 1;
								inQuote = true;
								break;
							case '*':
								if (text[i + 1] == '*')
								{
									i++;
									isUtf8 = true;
									goto case '"';
								}
								goto default;
							case '{':
								segmentStart = i + 1;
								inLiteralLength = true;
								break;
							case '(':
								listStack.Push(new List<IMessageData>());
								break;
							case '&':
								segmentStart = i + 1;
								inUtf7Escape = true;
								break;
							case ')':
							{
								List<IMessageData> currentList = listStack.Pop();
								listStack.Peek().Add(new ListMessageData(currentList));
								break;
							}
							default:
								segmentStart = i;
								break;
						}
					}
					else
					{
						switch (c)
						{
							case ' ':
							{
								string substring = text.Substring(segmentStart, i - segmentStart);
								if (substring == "NIL")
								{
									listStack.Peek().Add(NilMessageData.Value);
								}
								else
								{
									AddSegment(ref segment, substring);
									listStack.Peek().Add(new AtomMessageData(segment));
								}

								if (tag == null)
								{
									tag = segment;
								}

								segment = null;
								segmentStart = -1;
								break;
							}
							case '&':
							{
								if (isUtf8) goto default;
								string substring = text.Substring(segmentStart, i - segmentStart);
								AddSegment(ref segment, substring);
								segmentStart = i + 1;
								inUtf7Escape = true;
								break;
							}
							case ')':
							{
								string substring = text.Substring(segmentStart, i - segmentStart);
								if (substring == "NIL")
								{
									listStack.Peek().Add(NilMessageData.Value);
								}
								else
								{
									AddSegment(ref segment, substring);
									listStack.Peek().Add(new AtomMessageData(segment));
								}
								segment = null;
								segmentStart = -1;
								List<IMessageData> currentList = listStack.Pop();

								listStack.Peek().Add(new ListMessageData(currentList));
								break;
							}
							default:
								break;
						}
					}
				}
			}

			if (inLiteralLength || inQuote || inUtf7Escape)
			{
				throw new FormatException();
			}

			if (segmentStart != -1)
			{
				string substring = text.Substring(segmentStart);
				AddSegment(ref segment, substring);
				listStack.Peek().Add(new AtomMessageData(segment));
			}
		}

		private static void AddSegment(ref string result, string segment)
		{
			if (result == null)
			{
				result = segment;
			}
			else
			{
				result = result + segment;
			}
		}

		public void SetAuthenticatedUser(UserData userData)
		{
			AuthenticatedUser = userData;
			State = SessionState.Authenticated;
		}

		public async Task<SelectedMailbox> SelectMailboxAsync(Mailbox mailbox, CancellationToken cancellationToken)
		{
			State = SessionState.Selected;
			SelectedMailbox = await ProcessSelectionAsync(mailbox, cancellationToken);
			return SelectedMailbox;
		}

		private async Task<SelectedMailbox> ProcessSelectionAsync(Mailbox mailbox, CancellationToken cancellationToken)
		{
			return new SelectedMailbox(mailbox);
		}

		public async Task UnselectMailboxAsync(CancellationToken cancellationToken)
		{
			SelectedMailbox = null;
			State = SessionState.Authenticated;
		}

		public async Task EndCommandWithoutResponseAsync(IImapCommand command, CancellationToken cancellationToken)
		{
			_outstandingCommands.Remove(command);

			while (_pendingCommands.Count > 0 && CanRunImmediately(_pendingCommands.Peek()))
			{
				IImapCommand newCommand = _pendingCommands.Dequeue();
				_outstandingCommands.Add(newCommand);
				await newCommand.ExecuteAsync(this, cancellationToken);
			}
		}

		public void DiscardPendingExpungeResponses()
		{
			throw new NotImplementedException();
		}

		public Task SendAuthenticationFragmentAsync(byte[] data, CancellationToken cancellationToken)
		{
			return SendContinuationAsync(Convert.ToBase64String(data), Encoding.ASCII, cancellationToken);
		}

		public Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken)
		{
			return Connection.ReadLineAsync(Encoding.ASCII, cancellationToken)
					.ContinueWith(t => Convert.FromBase64String(t.Result), cancellationToken);
		}

		public string Id { get; }
		public Task RunAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}