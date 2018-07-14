using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Imap.Messages.Data;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[ImapCommand("LOGIN", SessionState.NotAuthenticated)]
	public class LoginCommand : BaseImapCommand
	{
		private readonly IImapMessageChannel _channel;
		private readonly IUserStore _userstore;
		private string _password;
		private string _userName;

		public LoginCommand(IImapMessageChannel channel, IUserStore userstore)
		{
			_channel = channel;
			_userstore = userstore;
		}

		protected override bool TryParseArguments(ImmutableList<IMessageData> arguments)
		{
			if (arguments.Count != 2)
			{
				return false;
			}

			_userName = MessageData.GetString(arguments[0], Encoding.UTF8);
			_password = MessageData.GetString(arguments[1], Encoding.UTF8);

			return true;
		}

		public override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			if (_channel.State != SessionState.NotAuthenticated)
			{
				await EndWithResultAsync(_channel, CommandResult.Bad, "LOGIN not valid at this point", cancellationToken);
				return;
			}

			UserData userData = await _userstore.GetUserWithPasswordAsync(_userName, _password, cancellationToken);

			if (userData == null)
			{
				await EndWithResultAsync(_channel, CommandResult.No, "credentials rejected", cancellationToken);
				return;
			}

			_channel.AuthenticatedUser = userData;
			await EndWithResultAsync(
				_channel,
				CommandResult.Ok,
				"login comleted, now in authenticated state",
				cancellationToken);
		}

		public override bool IsValidWith(IEnumerable<IImapCommand> commands)
		{
			return false;
		}
	}
}
