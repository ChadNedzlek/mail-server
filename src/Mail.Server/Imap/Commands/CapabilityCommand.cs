using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.Imap.Messages;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("CAPABILITY", SessionState.Open)]
	public class CapabilityCommand : BaseImapCommand
	{
		private readonly IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> _auth;
		private readonly IImapMessageChannel _channel;
		private readonly IConnectionSecurity _connection;

		public CapabilityCommand(
			IConnectionSecurity connection,
			IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> auth,
			IImapMessageChannel channel)
		{
			_connection = connection;
			_auth = auth;
			_channel = channel;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			return arguments.Count == 0;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			if (_connection.IsEncrypted)
			{
				var data = new List<IMessageData>
				{
					new AtomMessageData("IMAP4rev1")
				};


				foreach (Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata> mechanism in _auth)
				{
					data.Add(new AtomMessageData($"AUTH=${mechanism.Metadata.Name}"));
				}

				await _channel.SendMessageAsync(new ImapMessage(UntaggedTag, CommandName, data), cancellationToken);
			}
			else
			{
				await _channel.SendMessageAsync(
					new ImapMessage(
						UntaggedTag,
						CommandName,
						new AtomMessageData("IMAP4rev1"),
						new AtomMessageData("STARTTLS"),
						new AtomMessageData("LOGINDISABLED")
					),
					cancellationToken);
			}

			await EndOkAsync(_channel, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return true;
		}
	}
}
