using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MailServer;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp
{
	public class SmtpSession : IProtocolSession, IAuthenticationTransport, IDisposable, IMessageChannel, IMailBuilder
	{
	    private readonly ILifetimeScope _scope;

	    private SecurableConnection Connection { get; }
		private ConnectionInformation Addresses { get; set; }
		private SmtpSettings Settings { get; }
	    public string ConnectedHost { get; set; }

		public UserData AuthenticatedUser { get; set; }
		SmtpMailMessage IMailBuilder.PendingMail { get; set; }

		public bool IsAuthenticated => AuthenticatedUser != null;

		public SmtpSession(
			SecurableConnection connection,
			ConnectionInformation addresses,
			SmtpSettings settings,
			IContainer componentContext)
		{
			Connection = connection;
			Addresses = addresses;
			Settings = settings;
		    _scope = componentContext.BeginLifetimeScope();

		}

		public async Task RunAsync(CancellationToken token)
		{
			await this.SendReplyAsync(ReplyCode.Greeting, $"{Settings.DomainName} Service ready", token);
			while (!token.IsCancellationRequested)
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
			string line = await Connection.ReadLineAsync(Encoding.UTF8, token);
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

		    ICommand commandExecutor = _scope.ResolveKeyed<ICommand>(command);
		    if (commandExecutor == null)
			{
				await this.SendReplyAsync(ReplyCode.SyntaxError, "Command not implemented", token);
				return null;
			}

			commandExecutor.Initialize(arguments);

		    return commandExecutor;
		}

		Task IMessageChannel.SendReplyAsync(ReplyCode replyCode, bool more, string message, CancellationToken cancellationToken)
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

	    async Task IMessageChannel.SendReplyAsync(ReplyCode replyCode, IEnumerable<string> messages, CancellationToken cancellationToken)
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
			return this.SendReplyAsync(ReplyCode.AuthenticationFragment, Convert.ToBase64String(data), cancellationToken);
		}

		public async Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken)
		{
			return Convert.FromBase64String(await Connection.ReadLineAsync(Encoding.ASCII, cancellationToken));
		}

	    public void Dispose()
	    {
	        Connection?.Dispose();
	        _scope?.Dispose();
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

		public bool IsInvalid { get; private set; }

		public void Invalidate()
		{
			IsInvalid = true;
		}
	}
}
