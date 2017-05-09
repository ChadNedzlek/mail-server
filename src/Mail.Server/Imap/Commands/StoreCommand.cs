using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("STORE", SessionState.Selected)]
	public class StoreCommand : BaseImapCommand
	{
		private static readonly Regex ArgumentPattern = new Regex(@"^([+-]?)([A-Z]+)(.SILENT)?$", RegexOptions.IgnoreCase);
		private static readonly ImmutableList<string> KnownCommands = ImmutableList.CreateRange(new[] {"FLAGS"});
		private string _command;
		private NumberRangeMessageData _messageRange;
		private StoreOperation _operation;
		private bool _silent;
		private ListMessageData _valueList;
		private readonly IImapMailStore _mailstore;
		private readonly IImapMessageChannel _channel;
		private readonly IImapMailboxPointer _mailboxPointer;

		public StoreCommand(IImapMailStore mailstore, IImapMessageChannel channel, IImapMailboxPointer mailboxPointer)
		{
			_mailstore = mailstore;
			_channel = channel;
			_mailboxPointer = mailboxPointer;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 3) return false;

			string storeType = MessageData.GetString(arguments[0], Encoding.ASCII);
			_messageRange = arguments[1] as NumberRangeMessageData;
			_valueList = arguments[2] as ListMessageData;

			if (string.IsNullOrEmpty(storeType) ||
				_messageRange == null ||
				_valueList == null)
			{
				return false;
			}

			Match argumentMatch = ArgumentPattern.Match(storeType);

			if (!argumentMatch.Success)
			{
				return false;
			}

			switch (argumentMatch.Groups[1].Value)
			{
				case "":
					_operation = StoreOperation.Set;
					break;
				case "+":
					_operation = StoreOperation.Add;
					break;
				case "-":
					_operation = StoreOperation.Remove;
					break;
				default:
					return false;
			}

			_command = argumentMatch.Groups[2].Value;

			if (!KnownCommands.Contains(_command, StringComparer.OrdinalIgnoreCase))
			{
				return false;
			}

			_silent = string.Equals(argumentMatch.Groups[3].Value, ".SILENT", StringComparison.OrdinalIgnoreCase);

			return true;
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			var changedMessage = new List<MailMessage>();
			for (int i = _messageRange.Min; i <= _messageRange.Max; i++)
			{
				MailMessage message = await _mailboxPointer.SelectedMailbox.GetItemBySequenceAsync(i);
				ImmutableList<string> existing = ImmutableList.CreateRange(message.Flags);

				if (_silent || !message.Flags.SequenceEqual(existing, StringComparer.OrdinalIgnoreCase))
				{
					changedMessage.Add(message);
				}

				await _mailstore.RefreshAsync(message);
				IEnumerable<string> flagValues = _valueList.Items.Select(flag => MessageData.GetString(flag, Encoding.UTF8));
				switch (_operation)
				{
					case StoreOperation.Set:
						message.Flags.Clear();
						goto case StoreOperation.Add;
					case StoreOperation.Add:
						message.Flags.AddRange(flagValues);
						break;
					case StoreOperation.Remove:
						foreach (string flags in flagValues)
						{
							message.Flags.Remove(flags);
						}
						break;
				}

				await _mailstore.SaveAsync(message);
			}

			foreach (MailMessage message in changedMessage)
			{
				await
					_channel.SendMessageAsync(
						new ImapMessage(
							UntaggedTag,
							CommandName,
							new ListMessageData(
								new AtomMessageData("FLAGS"),
								new ListMessageData(message.Flags.Select(MessageData.CreateData)))),
						cancellationToken);
			}

			await EndOkAsync(_channel, cancellationToken);
		}

		private enum StoreOperation
		{
			Set,
			Add,
			Remove
		}
	}
}