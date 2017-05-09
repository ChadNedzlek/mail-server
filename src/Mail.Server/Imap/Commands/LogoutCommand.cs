using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LOGOUT", SessionState.Open)]
	public class LogoutCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;

		public LogoutCommand(IImapMessageChannel channel)
		{
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments) => arguments.Count == 0;

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			await _channel.SendMessageAsync(
				new ImapMessage(
					UntaggedTag,
					"BYE",
					new AtomMessageData("IMAP4rev1"),
					new ServerMessageData("Server logging out")),
				cancellationToken);

			await EndOkAsync(_channel, cancellationToken);
			_channel.EndSession();
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}
	}
}