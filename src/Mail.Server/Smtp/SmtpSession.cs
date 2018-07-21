using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSession : IProtocolSession, IDisposable, ISmtpMessageChannel, IMailBuilder
	{
		private readonly SecurableConnection _connection;
		private readonly IComponentContext _context;
		private readonly ILogger _log;
		private readonly AgentSettings _settings;

		private bool _closeRequested;

		public SmtpSession(
			SecurableConnection connection,
			AgentSettings settings,
			ConnectionInformation connectionInfo,
			IComponentContext context,
			ILogger log)
		{
			_context = context;
			_log = log;
			_connection = connection;
			_settings = settings;
			Id = $"SMTP {connectionInfo.RemoteAddress}";
		}

		public void Dispose()
		{
			_connection?.Dispose();
		}

		SmtpMailMessage IMailBuilder.PendingMail { get; set; }

		public string Id { get; }

		public async Task RunAsync(CancellationToken token)
		{
			await this.SendReplyAsync(SmtpReplyCode.Greeting, $"{_settings.DomainName} Service ready", token);
			while (!token.IsCancellationRequested && !_closeRequested)
			{
				ISmtpCommand command = await GetCommandAsync(token);
				if (command != null)
				{
					await command.ExecuteAsync(token);
				}
			}
		}

		public string ConnectedHost { get; set; }

		public UserData AuthenticatedUser { get; set; }

		public bool IsAuthenticated => AuthenticatedUser != null;

		Task ISmtpMessageChannel.SendReplyAsync(
			SmtpReplyCode smtpReplyCode,
			bool more,
			string message,
			CancellationToken cancellationToken)
		{
			var builder = new StringBuilder();
			builder.Append(((int) smtpReplyCode).ToString("D3"));
			builder.Append(more ? "-" : " ");
			if (message != null)
			{
				builder.Append(message);
			}
			
			string output = builder.ToString();
			_log.Verbose($"SMTP -> {output}");
			return _connection.WriteLineAsync(output, Encoding.ASCII, cancellationToken);
		}

		async Task ISmtpMessageChannel.SendReplyAsync(
			SmtpReplyCode smtpReplyCode,
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
					builder.Append(((int) smtpReplyCode).ToString("D3"));
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
			builder.Append(((int) smtpReplyCode).ToString("D3"));
			builder.Append(" ");
			if (message != null)
			{
				builder.Append(message);
			}

			var output = builder.ToString();
			_log.Verbose($"SMTP -> {output}");
			await _connection.WriteLineAsync(output, Encoding.ASCII, cancellationToken);
		}

		void ISmtpMessageChannel.Close()
		{
			_closeRequested = true;
			_connection.Close();
		}

		private async Task<ISmtpCommand> GetCommandAsync(CancellationToken token)
		{
			string line = await _connection.ReadLineAsync(Encoding.UTF8, token);
			_log.Verbose($"SMTP <- {line}");
			if (line.Length < 4)
			{
				await this.SendReplyAsync(SmtpReplyCode.SyntaxError, "No command found", token);
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

			var commandExecutor = _context.ResolveOptionalKeyed<ISmtpCommand>(command);
			if (commandExecutor == null)
			{
				await this.SendReplyAsync(SmtpReplyCode.SyntaxError, "Command not implemented", token);
				return null;
			}

			commandExecutor.Initialize(arguments);

			return commandExecutor;
		}
	}

	public class SmtpPath
	{
		public SmtpPath(string mailbox)
		{
			Mailbox = mailbox;
		}

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
	}
}
