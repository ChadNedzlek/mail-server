using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("AUTH")]
	public class AuthenticateCommand : BaseSmtpCommand
	{
		private readonly IIndex<string, IAuthenticationSession> _authentication;
		private readonly ISmtpMessageChannel _channel;

		public AuthenticateCommand(
			IIndex<string, IAuthenticationSession> authentication,
			ISmtpMessageChannel channel)
		{
			_authentication = authentication;
			_channel = channel;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			string[] parts = Arguments.Split(new[] {' '}, 2);
			if (parts == null || parts.Length == 0 || parts.Length > 2)
			{
				await _channel.SendReplyAsync(
					SmtpReplyCode.InvalidArguments,
					"Expected mechanism and optional initial response",
					token);
				return;
			}
			string mechanismName = parts[0];

			if (!_authentication.TryGetValue(mechanismName, out var mechanism))
			{
				await _channel.SendReplyAsync(SmtpReplyCode.InvalidArguments, "Unknown mechanism", token);
				return;
			}

			UserData userData;

			try
			{
				userData = await mechanism.AuthenticateAsync(false, token);
			}
			catch (ArgumentException)
			{
				await _channel.SendReplyAsync(SmtpReplyCode.InvalidArguments, "Invalid arguments", token);
				return;
			}

			_channel.AuthenticatedUser = userData;
			await _channel.SendReplyAsync(SmtpReplyCode.AuthenticationComplete, "Authentication unsuccessful", token);
		}
	}
}
