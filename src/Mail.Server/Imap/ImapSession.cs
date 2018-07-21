using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Autofac.Features.Metadata;
using Autofac.Features.OwnedInstances;
using JetBrains.Annotations;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Imap.Commands;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Imap
{
	[Injected]
	public sealed class ImapSession : IDisposable,
		IProtocolSession,
		IAuthenticationTransport,
		IImapMessageChannel,
		IImapMailboxPointer
	{
		private readonly IIndex<string, Meta<Lazy<Owned<IImapCommand>>, ImapCommandMetadata>> _commands;

		private readonly SecurableConnection _connection;
		private readonly List<IImapCommand> _outstandingCommands = new List<IImapCommand>();
		private readonly Queue<IImapCommand> _pendingCommands = new Queue<IImapCommand>();
		private readonly byte[] _readBuffer = new byte[1000000];
		private readonly SemaphoreSlim _readSemaphore = new SemaphoreSlim(1);
		private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1);
		private readonly ILogger _logger;

		public ImapSession(
			[NotNull] SecurableConnection connection,
			[NotNull] IIndex<string, Meta<Lazy<Owned<IImapCommand>>, ImapCommandMetadata>> commands,
			ILogger logger)
		{
			_commands = commands ?? throw new ArgumentNullException(nameof(commands));
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
			_logger = logger;
			State = SessionState.Open;
		}

		public Task SendAuthenticationFragmentAsync(byte[] data, CancellationToken cancellationToken)
		{
			return SendContinuationAsync(Convert.ToBase64String(data), Encoding.ASCII, cancellationToken);
		}

		public Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken)
		{
			return _connection.ReadLineAsync(Encoding.ASCII, cancellationToken)
				.ContinueWith(t => Convert.FromBase64String(t.Result), cancellationToken);
		}

		public void Dispose()
		{
			_connection.Dispose();
		}

		public SelectedMailbox SelectedMailbox { get; private set; }

		public async Task<SelectedMailbox> SelectMailboxAsync(Mailbox mailbox, CancellationToken cancellationToken)
		{
			State = SessionState.Selected;
			SelectedMailbox = await ProcessSelectionAsync(mailbox, cancellationToken);
			return SelectedMailbox;
		}

		public async Task UnselectMailboxAsync(CancellationToken cancellationToken)
		{
			SelectedMailbox = null;
			State = SessionState.Authenticated;
		}

		public SessionState State { get; set; }
		public UserData AuthenticatedUser { get; set; }

		public void EndSession()
		{
			State = SessionState.Closed;
			_connection.Close();
		}

		public async Task CommandCompletedAsync(
			ImapMessage message,
			IImapCommand command,
			CancellationToken cancellationToken)
		{
			await SendPendingResponsesAsync(cancellationToken);
			await this.SendMessageAsync(message, cancellationToken);
			await EndCommandWithoutResponseAsync(command, cancellationToken);
		}

		public async Task SendMessageAsync(ImapMessage message, Encoding encoding, CancellationToken cancellationToken)
		{
			using (await SemaphoreLock.GetLockAsync(_sendSemaphore, cancellationToken))
			{
				var builder = new StringBuilder();
				builder.Append(message.Tag);
				foreach (IMessageData data in message.Data)
				{
					if (data is LiteralMessageData literal)
					{
						builder.Append("}");
						builder.Append(literal.Length);
						builder.AppendLine("}");
						await _connection.WriteAsync(builder.ToString(), encoding, cancellationToken);
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
					_logger.Verbose("IMAP -> " + builder);
					builder.AppendLine();
					await _connection.WriteAsync(builder.ToString(), encoding, cancellationToken);
				}
			}
		}

		public async Task EndCommandWithoutResponseAsync(IImapCommand command, CancellationToken cancellationToken)
		{
			_outstandingCommands.Remove(command);

			while (_pendingCommands.Count > 0 && CanRunImmediately(_pendingCommands.Peek()))
			{
				IImapCommand newCommand = _pendingCommands.Dequeue();
				_outstandingCommands.Add(newCommand);
				await newCommand.ExecuteAsync(cancellationToken);
			}
		}

		public void DiscardPendingExpungeResponses()
		{
			throw new NotImplementedException();
		}

		public string Id { get; }

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			await SendMessageAsync(new ImapMessage("*", "OK", new ServerMessageData("IMAP4rev1 Service Ready")), Encoding.ASCII, cancellationToken);

			State = SessionState.NotAuthenticated;

			cancellationToken.ThrowIfCancellationRequested();
			while (_connection.State != SecurableConnectionState.Closed)
			{
				cancellationToken.ThrowIfCancellationRequested();
				IImapCommand command;
				try
				{
					command = await ReadCommandAsync(cancellationToken);
				}
				catch (BadImapCommandFormatException e)
				{
					await ImapMessageChannel.ReportBadAsync(this, e.Tag, e.ErrorMessage ?? "Invalid command", cancellationToken);
					continue;
				}

				if (!command.HasValidArguments)
				{
					await ImapMessageChannel.ReportBadAsync(this, command.Tag, "Invalid command", cancellationToken);
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

				await command.ExecuteAsync(cancellationToken);
			}
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		private bool CanRunImmediately(IImapCommand command)
		{
			if (!command.IsValidWith(_outstandingCommands))
			{
				return false;
			}

			IImapCommand[] commandEnumerable = {command};
			if (!_outstandingCommands.All(c => c.IsValidWith(commandEnumerable)))
			{
				return false;
			}

			return true;
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
				string line = await _connection.ReadLineAsync(Encoding.UTF8, cancellationToken);
				_logger.Verbose("IMAP <- " + line);
				ParseLine(ref tag, line, data);
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
				command.Value.Value.Dispose();
				throw new BadImapCommandFormatException(tagArg.Value);
			}

			IImapCommand imapCommand = command.Value.Value.Value;
			imapCommand.Initialize(commandArg.Value, tagArg.Value, data.Skip(2).ToImmutableList());
			return imapCommand;
		}

		public async Task SendContinuationAsync(string text, Encoding encoding, CancellationToken cancellationToken)
		{
			await _connection.WriteLineAsync("+ " + text, encoding, cancellationToken);
		}

		internal static void ParseLine(string text, List<IMessageData> data)
		{
			string tag = null;
			ParseLine(ref tag, text, data);
		}

		internal static void ParseLine(ref string tag, string text, List<IMessageData> data)
		{
			int segmentStart = -1;
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
						if (!int.TryParse(text.Substring(segmentStart, i - segmentStart), out int literalLength))
						{
							throw new BadImapCommandFormatException(tag);
						}

						if (i != text.Length - 1)
						{
							throw new BadImapCommandFormatException(tag);
						}

						listStack.Peek().Add(new LiteralMessageData(literalLength));

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
								if (isUtf8)
								{
									goto default;
								}

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

		public async Task<IVariableStreamReader> ReadLiteralDataAsync(CancellationToken cancellationToken)
		{ 
			await _connection.WriteLineAsync("+ Ready for literal data", Encoding.ASCII, cancellationToken);
			return _connection;
		}

		private async Task<SelectedMailbox> ProcessSelectionAsync(Mailbox mailbox, CancellationToken cancellationToken)
		{
			return new SelectedMailbox(mailbox);
		}
	}
}
