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
		private readonly IMailBuilder _builder;
		private readonly ISmtpMessageChannel _channel;

		public AuthenticateCommand(
			IIndex<string, IAuthenticationSession> authentication,
			ISmtpMessageChannel channel,
			IMailBuilder builder)
		{
			_authentication = authentication;
			_channel = channel;
			_builder = builder;
		}

		public override async Task ExecuteAsync(CancellationToken token)
		{
			if (_channel.IsAuthenticated)
			{
				await _channel.SendReplyAsync(SmtpReplyCode.BadSequence, "AUTH already recieved", token);
				return;
			}

			if (_builder.PendingMail != null)
			{
				await _channel.SendReplyAsync(SmtpReplyCode.BadSequence, "AUTH not allowed during MAIL transaction", token);
				return;
			}

			string[] parts = Arguments.Split(new[] {' '}, 2);
			if (parts.Length == 0 || parts.Length > 1)
			{
				await _channel.SendReplyAsync(
					SmtpReplyCode.InvalidArguments,
					"Expected mechanism and optional initial response",
					token);
				return;
			}

			string mechanismName = parts[0];

			if (!_authentication.TryGetValue(mechanismName, out IAuthenticationSession mechanism))
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

			if (userData == null)
			{
				await _channel.SendReplyAsync(SmtpReplyCode.AuthencticationCredentialsInvalid, "Authentication failed", token);
				return;
			}

			_channel.AuthenticatedUser = userData;
			await _channel.SendReplyAsync(SmtpReplyCode.AuthenticationComplete, "Authentication successful", token);
		}
	}
}
