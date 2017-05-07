using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	public abstract class BaseImapCommand : IImapCommand
	{
		public const string UntaggedTag = "*";

		public void Initialize(string commandName, string tag, ImmutableList<IMessageData> data)
		{
			CommandName = commandName;
			Tag = tag;
			Arguments = data;
			HasValidArguments = TryParseArguments(Arguments);
		}

		public string Tag { get; private set; }
		public ImmutableList<IMessageData> Arguments { get; private set; }
		public bool HasValidArguments { get; private set; }
		public string CommandName { get; private set; }

		public abstract bool IsValidWith(IEnumerable<IImapCommand> commands);
		public abstract Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken);
		protected abstract bool TryParseArguments(ImmutableList<IMessageData> arguments);

		protected Message GetResultMessage(CommandResult result, params IMessageData[] data)
		{
			return new Message(Tag, result.ToString().ToUpperInvariant(), data);
		}

		internal Message GetOkMessage(string text = null)
		{
			if (text == null)
			{
				text = CommandName + " completed";
			}
			return GetResultMessage(CommandResult.Ok, new ServerMessageData(text));
		}

		internal Message GetNoMessage(string text)
		{
			return GetResultMessage(CommandResult.No, new ServerMessageData(text));
		}

		internal Message GetBadMessage(string text)
		{
			return GetResultMessage(CommandResult.Bad, new ServerMessageData(text));
		}

		protected Task EndWithResultAsync(
			ImapSession session,
			CommandResult result,
			string text,
			CancellationToken cancellationToken)
		{
			return EndWithResultAsync(session, result, null, text, cancellationToken);
		}

		protected Task EndOkAsync(ImapSession session, CancellationToken cancellationToken)
		{
			return EndWithResultAsync(session, CommandResult.Ok, null, cancellationToken);
		}

		protected Task EndOkAsync(ImapSession session, string message, CancellationToken cancellationToken)
		{
			return EndWithResultAsync(session, CommandResult.Ok, message, cancellationToken);
		}

		protected async Task EndWithResultAsync(
			ImapSession session,
			CommandResult result,
			TagMessageData tags,
			string text,
			CancellationToken cancellationToken)
		{
			if (result == CommandResult.Ok && text == null)
			{
				text = CommandName + " completed";
			}

			if (tags == null)
			{
				await session.CommandCompletedAsync(GetResultMessage(result, new ServerMessageData(text)), this, cancellationToken);
			}
			else
			{
				await
					session.CommandCompletedAsync(GetResultMessage(result, tags, new ServerMessageData(text)), this, cancellationToken);
			}
		}
	}
}