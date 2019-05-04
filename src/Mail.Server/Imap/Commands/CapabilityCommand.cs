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
		private readonly IEnumerable<Lazy<IAuthenticationSession, AuthencticationMechanismMetadata>> _auth;
		private readonly IImapMessageChannel _channel;
		private readonly IConnectionSecurity _connection;

		public CapabilityCommand(
			IConnectionSecurity connection,
			IEnumerable<Lazy<IAuthenticationSession, AuthencticationMechanismMetadata>> auth,
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
			var data = new List<IMessageData>
			{
				new AtomMessageData("IMAP4rev1")
			};

			if (!_connection.IsEncrypted && _connection.CanEncrypt)
				data.Add(new AtomMessageData("STARTTLS"));

			bool validLogin = false;
			foreach (Lazy<IAuthenticationSession, AuthencticationMechanismMetadata> mechanism in _auth)
			{
				if (!mechanism.Metadata.RequiresEncryption || _connection.IsEncrypted)
				{
					data.Add(new AtomMessageData($"AUTH=${mechanism.Metadata.Name}"));
					validLogin = true;
				}
			}

			if (!validLogin && !_connection.IsEncrypted)
			{
				data.Add(new AtomMessageData("LOGINDISABLED"));
			}

			await _channel.SendMessageAsync(new ImapMessage(UntaggedTag, CommandName, data), cancellationToken);

			await EndOkAsync(_channel, cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return true;
		}
	}
}
