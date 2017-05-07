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

		public CapabilityCommand(
			IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> auth)
		{
			_auth = auth;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments) => arguments.Count == 0;

		public override async Task ExecuteAsync(ImapSession session, CancellationToken cancellationToken)
		{
			if (session.Connection.IsEncrypted)
			{
				var data = new List<IMessageData>
				{
					new AtomMessageData("IMAP4rev1")
				};


				foreach (var mechanism in _auth)
				{
					data.Add(new AtomMessageData($"AUTH=${mechanism.Metadata.Name}"));
				}

				await session.SendMessageAsync(new Message(UntaggedTag, CommandName, data), cancellationToken);
			}
			else
			{
				await session.SendMessageAsync(
					new Message(
						UntaggedTag,
						CommandName,
						new AtomMessageData("IMAP4rev1"),
						new AtomMessageData("STARTTLS"),
						new AtomMessageData("LOGINDISABLED")
					),
					cancellationToken);
			}

			await EndOkAsync(session, cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return true;
		}
	}
}