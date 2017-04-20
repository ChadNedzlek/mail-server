using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSession : IProtocolSession, IAuthenticationTransport, IDisposable, IMessageChannel, IMailBuilder
	{
		private readonly IComponentContext _context;
		private readonly SecurableConnection _connection;
		private readonly SmtpSettings _settings;

		private bool _closeRequested;

		public SmtpSession(
			SecurableConnection connection,
			SmtpSettings settings,
			ConnectionInformation connectionInfo,
			IComponentContext context)
		{
			_context = context;
			_connection = connection;
			_settings = settings;
			Id = $"SMTP {connectionInfo.RemoteAddress}";
		}

		public Task SendAuthenticationFragmentAsync(byte[] data, CancellationToken cancellationToken)
		{
			return this.SendReplyAsync(ReplyCode.AuthenticationFragment, Convert.ToBase64String(data), cancellationToken);
		}

		public async Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken)
		{
			return Convert.FromBase64String(await _connection.ReadLineAsync(Encoding.ASCII, cancellationToken));
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}

		SmtpMailMessage IMailBuilder.PendingMail { get; set; }
		public string ConnectedHost { get; set; }

		public UserData AuthenticatedUser { get; set; }

		public bool IsAuthenticated => AuthenticatedUser != null;

		Task IMessageChannel.SendReplyAsync(
			ReplyCode replyCode,
			bool more,
			string message,
			CancellationToken cancellationToken)
		{
			var builder = new StringBuilder();
			builder.Append(((int) replyCode).ToString("D3"));
			builder.Append(more ? "-" : " ");
			if (message != null)
			{
				builder.Append(message);
			}
			return _connection.WriteLineAsync(builder.ToString(), Encoding.ASCII, cancellationToken);
		}

		async Task IMessageChannel.SendReplyAsync(
			ReplyCode replyCode,
			IEnumerable<string> messages,
			CancellationToken cancellationToken)
		{
			string message;
			StringBuilder builder;
			using (IEnumerator<string> enumerator = messages.GetEnumerator())
			{
				if (!enumerator.MoveNext())
				{
					throw new ArgumentException("at least one response is required", nameof(messages));
				}

				message = enumerator.Current;
				bool more = enumerator.MoveNext();
				builder = new StringBuilder();
				while (more)
				{
					builder.Clear();
					builder.Append(((int) replyCode).ToString("D3"));
					builder.Append("-");
					if (message != null)
					{
						builder.Append(message);
					}
					await _connection.WriteLineAsync(builder.ToString(), Encoding.ASCII, cancellationToken);
					message = enumerator.Current;
					more = enumerator.MoveNext();
				}
			}

			builder.Clear();
			builder.Append(((int) replyCode).ToString("D3"));
			builder.Append(" ");
			if (message != null)
			{
				builder.Append(message);
			}
			await _connection.WriteLineAsync(builder.ToString(), Encoding.ASCII, cancellationToken);
		}

		void IMessageChannel.Close()
		{
			_closeRequested = true;
			_connection.Close();
		}

		public string Id { get; }

		public async Task RunAsync(CancellationToken token)
		{
			await this.SendReplyAsync(ReplyCode.Greeting, $"{_settings.DomainName} Service ready", token);
			while (!token.IsCancellationRequested && !_closeRequested)
			{
				ICommand command = await GetCommandAsync(token);
				if (command != null)
				{
					await command.ExecuteAsync(token);
				}
			}
		}

		private async Task<ICommand> GetCommandAsync(CancellationToken token)
		{
			string line = await _connection.ReadLineAsync(Encoding.UTF8, token);
			if (line.Length < 4)
			{
				await this.SendReplyAsync(ReplyCode.SyntaxError, "No command found", token);
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

			var commandExecutor = _context.ResolveOptionalKeyed<ICommand>(command);
			if (commandExecutor == null)
			{
				await this.SendReplyAsync(ReplyCode.SyntaxError, "Command not implemented", token);
				return null;
			}

			commandExecutor.Initialize(arguments);

			return commandExecutor;
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
		public SmtpMailMessage(SmtpPath fromPath)
		{
			FromPath = fromPath;
		}

		public SmtpPath FromPath { get; }
		public List<string> Recipents { get; } = new List<string>();
		public bool IsBinary { get; set; }

		public bool IsInvalid { get; private set; }

		public void Invalidate()
		{
			IsInvalid = true;
		}
	}
}
