using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailServer;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSession : IProtocolSession, IAuthenticationTransport
	{
		public SecurableConnection Connection { get; }
		public SmtpImplementationFactory ImplementationFactory { get; }
		public SmtpSettings Settings { get; }
		public IMailStore MailStore { get; }
		public IUserStore UserStore { get; }
		public string ConnectedHost { get; set; }
		public string ConnectedIpAddress { get; }
		public string IpAddress { get; }

		public UserData AuthenticatedUser { get; set; }
		public bool IsAuthenticated => AuthenticatedUser != null;
		public SmtpMailMessage PendingMail { get; set; }

		public SmtpSession(
			SecurableConnection connection,
			SmtpImplementationFactory implementationFactory,
			SmtpSettings settings,
			string ipAddress,
			string connectedIpAddress,
			IMailStore mailStore,
			IUserStore userStore)
		{
			Connection = connection;
			ImplementationFactory = implementationFactory;
			Settings = settings;
			IpAddress = ipAddress;
			ConnectedIpAddress = connectedIpAddress;
			MailStore = mailStore;
			UserStore = userStore;
		}

		public async Task Start(CancellationToken token)
		{
			await SendReplyAsync(ReplyCode.Greeting, $"{Settings.DomainName} Service ready", token);
			while (!token.IsCancellationRequested)
			{
				ICommand command = await GetCommandAsync(token);
				if (command != null) await command.ExecuteAsync(this, token);
			}
		}

		private async Task<ICommand> GetCommandAsync(CancellationToken token)
		{
			string line = await Connection.ReadLineAsync(Encoding.UTF8, token);
			if (line.Length < 4)
			{
				await SendReplyAsync(ReplyCode.SyntaxError, "No command found", token);
				return null;
			}

			int spaceIndex = line.IndexOf(" ", StringComparison.Ordinal);
			string command;
			string arguments;
			if (spaceIndex == -1)
			{
				command = line;
				arguments = null;
			}
			else
			{
				command = line.Substring(0, spaceIndex);
				arguments = line.Substring(spaceIndex + 1);
			}

			ICommandFactory commandFactory = ImplementationFactory.Get(command);
			if (commandFactory == null)
			{
				await SendReplyAsync(ReplyCode.SyntaxError, "Command not implemented", token);
				return null;
			}

			return commandFactory.CreateCommand(arguments);
		}

		public Task SendReplyAsync(ReplyCode replyCode, CancellationToken cancellationToken)
		{
			return SendReplyAsync(replyCode, (string)null, cancellationToken);
		}

		public Task SendReplyAsync(ReplyCode replyCode, string message, CancellationToken cancellationToken)
		{
			return SendReplyAsync(replyCode, false, message, cancellationToken);
		}

		public Task SendReplyAsync(ReplyCode replyCode, bool more, string message, CancellationToken cancellationToken)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(((int)replyCode).ToString("D3"));
			builder.Append(more ? "-" : " ");
			if (message != null)
			{
				builder.Append(message);
			}
			return Connection.WriteLineAsync(builder.ToString(), Encoding.ASCII, cancellationToken);
		}

		public async Task SendReplyAsync(ReplyCode replyCode, IEnumerable<string> messages, CancellationToken cancellationToken)
		{
			IEnumerator<string> enumerator = messages.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				throw new ArgumentException("at least one response is required", nameof(messages));
			}

			string message = enumerator.Current;
			bool more = enumerator.MoveNext();
			StringBuilder builder = new StringBuilder();
			while (more)
			{
				builder.Clear();
				builder.Append(((int)replyCode).ToString("D3"));
				builder.Append("-");
				if (message != null)
				{
					builder.Append(message);
				}
				await Connection.WriteLineAsync(builder.ToString(), Encoding.ASCII, cancellationToken);
				message = enumerator.Current;
				more = enumerator.MoveNext();
			}

			builder.Clear();
			builder.Append(((int)replyCode).ToString("D3"));
			builder.Append(" ");
			if (message != null)
			{
				builder.Append(message);
			}
			await Connection.WriteLineAsync(builder.ToString(), Encoding.ASCII, cancellationToken);
		}

		public Task CloseAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task SendAuthenticationFragmentAsync(byte[] data, CancellationToken cancellationToken)
		{
			return SendReplyAsync(ReplyCode.AuthenticationFragment, Convert.ToBase64String(data), cancellationToken);
		}

		public async Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken)
		{
			return Convert.FromBase64String(await Connection.ReadLineAsync(Encoding.ASCII, cancellationToken));
		}
	}

	public class SmtpPath
	{
		public SmtpPath(ImmutableList<string> sourceRoute, string mailbox)
		{
			SourceRoute = sourceRoute;
			Mailbox = mailbox;
		}

		public ImmutableList<string> SourceRoute { get; }
		public string Mailbox { get; }
	}

	public class SmtpMailMessage
	{
		public SmtpPath FromPath { get; }
		public List<string> Recipents { get; } = new List<string>();
		public bool IsBinary { get; set; }

		public SmtpMailMessage(SmtpPath fromPath)
		{
			FromPath = fromPath;
		}
	}
}