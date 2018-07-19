using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("AUTHENTICATE", SessionState.NotAuthenticated)]
	public class AuthenticateCommand : BaseImapCommand
	{
		private readonly IIndex<string, Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> _auth;
		private readonly IImapMessageChannel _channel;

		public AuthenticateCommand(
			IIndex<string, Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> auth,
			IImapMessageChannel channel)
		{
			_auth = auth;
			_channel = channel;
		}

		public string Mechanism { get; private set; }

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			if (!_auth.TryGetValue(Mechanism, out Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata> mechanism))
			{
				await EndWithResultAsync(_channel, CommandResult.No, "unknown mechanism", cancellationToken);
				return;
			}

			if (mechanism.Metadata.RequiresEncryption)
			{
				if (_channel.State != SessionState.NotAuthenticated)
				{
					await EndWithResultAsync(_channel, CommandResult.Bad, "AUTHENTCATE not valid at this time", cancellationToken);
					return;
				}
			}
			else
			{
				if (_channel.State != SessionState.Open && _channel.State != SessionState.NotAuthenticated)
				{
					await EndWithResultAsync(_channel, CommandResult.Bad, "AUTHENTCATE not valid at this time", cancellationToken);
					return;
				}
			}

			IAuthenticationSession authSession = mechanism.Value;

			UserData userData;
			try
			{
				userData = await authSession.AuthenticateAsync(false, cancellationToken);
			}
			catch (ArgumentException)
			{
				await EndWithResultAsync(_channel, CommandResult.Bad, "invalid arguments", cancellationToken);
				return;
			}

			if (userData == null)
			{
				await EndWithResultAsync(_channel, CommandResult.No, "credentials rejected", cancellationToken);
				return;
			}

			_channel.AuthenticatedUser = userData;

			await
				EndWithResultAsync(
					_channel,
					CommandResult.Ok,
					$"{Mechanism} authenticate completed, now in authenticated state",
					cancellationToken);
		}

		protected override bool IsValidWithCommands(IReadOnlyList<IImapCommand> commands)
		{
			return false;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 1)
			{
				return false;
			}

			Mechanism = MessageData.GetString(arguments[0], Encoding.ASCII);
			if (string.IsNullOrEmpty(Mechanism))
			{
				return false;
			}

			return true;
		}
	}
}
