using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[SmtpCommand("EHLO")]
	public class ExtendedHelloCommand : BaseSmtpCommand
	{
		private static readonly string[] s_generalExtensions =
		{
			//"8BITMIME",
			//"UTF8SMTP",
			//"SMTPUTF8",
			//"CHUNKING",
			//"BINARYMIME",
		};

		private readonly IList<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> _authentication;
		private readonly ISmtpMessageChannel _channel;
		private readonly IConnectionSecurity _connection;
		private readonly ILogger _log;
		private readonly SmtpSettings _settings;

		public ExtendedHelloCommand(
			IEnumerable<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> authentication,
			IConnectionSecurity connection,
			ISmtpMessageChannel channel,
			SmtpSettings settings,
			ILogger log)
		{
			_authentication = authentication.ToList();
			_connection = connection;
			_channel = channel;
			_settings = settings;
			_log = log;
		}


		public override async Task ExecuteAsync(CancellationToken token)
		{
			_channel.ConnectedHost = Arguments;

			IEnumerable<string> extensions = s_generalExtensions;
			if (_connection.IsEncrypted)
			{
				if (_authentication.Count > 0)
				{
					extensions = extensions.Append("AUTH " + string.Join(" ", _authentication.Select(a => a.Metadata.Name)));
				}
			}
			else
			{
				List<Lazy<IAuthenticationSession, IAuthencticationMechanismMetadata>> plainAuths =
					_authentication.Where(a => !a.Metadata.RequiresEncryption).ToList();
				if (plainAuths.Count > 0)
				{
					extensions = extensions.Append("AUTH " + string.Join(" ", plainAuths.Select(a => a.Metadata.Name)));
				}

				if (_connection.Certificate != null)
				{
					extensions = extensions.Concat(new[] {"STARTTLS"});
				}
			}

			_log.Information($"EHLO from {Arguments} {(_connection.IsEncrypted ? "encrytped" : "unencrypted")}");

			List<string> extentionList = extensions.ToList();

			if (extentionList.Any())
			{
				await _channel.SendReplyAsync(
					ReplyCode.Okay,
					true,
					$"{_settings.DomainName} greets {Arguments}",
					token);
				await _channel.SendReplyAsync(ReplyCode.Okay, extentionList, token);
			}
			else
			{
				await _channel.SendReplyAsync(
					ReplyCode.Okay,
					false,
					$"{_settings.DomainName} greets {Arguments}",
					token);
			}
		}
	}
}
