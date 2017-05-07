using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Vaettir.Mail.Smtp.Test
{
	public class SmtpSessionTest
	{
		public SmtpSessionTest(ITestOutputHelper output)
		{
			_output = output;
		}

		private readonly ITestOutputHelper _output;
		private static readonly X509Certificate2 s_serverCert = TestHelpers.GetSelfSigned();

		private class TestConnection : IDisposable
		{
			public readonly CancellationTokenSource CancellationTokenSource;
			public readonly SecurableConnection Connection;
			public readonly RedirectableStream LocalStream;
			public readonly StreamReader Reader;
			public readonly SmtpSession Session;
			public readonly Task SessionTask;
			public readonly StreamWriter Writer;

			public TestConnection(ITestOutputHelper output)
			{
				var (a, b) = PairedStream.Create();
				LocalStream = new RedirectableStream(a);
				Reader = new StreamReader(LocalStream);
				Writer = new StreamWriter(LocalStream) {AutoFlush = true};


				var builder = new ContainerBuilder();
				builder.RegisterAssemblyTypes(typeof(SmtpSession).GetTypeInfo().Assembly)
					.Where(t => t.GetTypeInfo().GetCustomAttribute<SmtpCommandAttribute>() != null)
					.Keyed<ISmtpCommand>(t => t.GetTypeInfo().GetCustomAttribute<SmtpCommandAttribute>().Name);
				builder.RegisterInstance(TestHelpers.MakeSettings(domainName: "test.vaettir.net"))
					.As<SmtpSettings>()
					.As<ProtocolSettings>();

				builder.RegisterType<SmtpSession>()
					.As<SmtpSession>()
					.As<IMessageChannel>()
					.As<IMailBuilder>()
					.As<IAuthenticationTransport>()
					.As<IProtocolSession>();

				builder.RegisterInstance(new SecurableConnection(b) {Certificate = s_serverCert})
					.As<IConnectionSecurity>()
					.As<SecurableConnection>();

				builder.RegisterInstance(new TestOutputLogger(output))
					.As<ILogger>();

				builder.RegisterInstance(new ConnectionInformation("127.0.0.1", "128.0.0.1"));

				Container = builder.Build();

				Connection = Container.Resolve<SecurableConnection>();
				Session = Container.Resolve<SmtpSession>();

				CancellationTokenSource = new CancellationTokenSource();
				CancellationToken token = CancellationTokenSource.Token;
				SessionTask = Task.Run(() => Session.RunAsync(token), token);
			}

			public IContainer Container { get; }

			public void Dispose()
			{
				LocalStream?.Dispose();
				Reader?.Dispose();
				Writer?.Dispose();
				Connection?.Dispose();
				Session?.Dispose();
			}

			internal async Task EndConversation()
			{
				Session.Dispose();
				await TaskHelpers.AssertTriggered(SessionTask);
			}

			internal async Task<Match> ConverseAsync(Txn txn)
			{
				switch (txn.Direction)
				{
					case TxnDirection.ToServer:
						await ToAsync(txn.Message);
						return null;
					case TxnDirection.FromServer:
						return await FromAsync(txn.Message);
				}

				Assert.Contains(txn.Direction, new[] {TxnDirection.FromServer, TxnDirection.ToServer});
				throw new Exception(); // unreachable
			}

			internal async Task<Match> FromAsync(string pattern)
			{
				var totalLines = "";
				string line;
				do
				{
					totalLines += (line = await Do(Reader.ReadLineAsync())) + "\n";
				} while (line[3] == '-');
				var rx = new Regex(pattern, RegexOptions.Multiline);
				Match match = rx.Match(totalLines);
				Assert.True(match.Success, $"Conversation '{totalLines}' line matches pattern '{pattern}'");
				return match;
			}

			public async Task<T> Do<T>(Task<T> func)
			{
				Task task = await Task.WhenAny(func, SessionTask);

				if (task == SessionTask)
				{
					throw task.Exception?.InnerExceptions.FirstOrDefault() ??
						task.Exception ??
						(Exception) new InvalidOperationException("Session should not have completed.");
				}

				return await func;
			}

			public async Task Do(Task func)
			{
				Task task = await Task.WhenAny(func, SessionTask);

				if (task == SessionTask)
				{
					throw task.Exception?.InnerExceptions.FirstOrDefault() ??
						task.Exception ??
						(Exception) new InvalidOperationException("Session should not have completed.");
				}
			}

			internal async Task ToAsync(string message)
			{
				await Writer.WriteLineAsync(message);
				await Writer.FlushAsync();
			}
		}

		private async Task ConversationTest(params Txn[] conversation)
		{
			using (var conn = new TestConnection(_output))
			{
				await HaveConversation(conn, conversation);

				await conn.EndConversation();
			}
		}

		private async Task HaveConversation(TestConnection connection, params Txn[] conversation)
		{
			await HaveConversation(connection, (IEnumerable<Txn>) conversation);
		}

		private async Task HaveConversation(TestConnection connection, IEnumerable<Txn> conversation)
		{
			foreach (Txn txn in conversation)
			{
				await connection.ConverseAsync(txn);
			}
		}

		private enum TxnDirection
		{
			ToServer,
			FromServer
		}

		private class Txn
		{
			public Txn(TxnDirection direction, string message)
			{
				Direction = direction;
				Message = message;
			}

			public TxnDirection Direction { get; }
			public string Message { get; }

			public static Txn To(string message)
			{
				return new Txn(TxnDirection.ToServer, message);
			}

			public static Txn From(string message)
			{
				return new Txn(TxnDirection.FromServer, message);
			}
		}

		[Fact]
		public async Task EhloTest()
		{
			await ConversationTest(
				Txn.From("^220 "),
				Txn.To("EHLO test.com"),
				Txn.From(@"\A(250-.*\n)*250 .*\Z")
			);
		}

		[Fact]
		public async Task HeloTest()
		{
			await ConversationTest(
				Txn.From("^220 "),
				Txn.To("HELO test.com"),
				Txn.From("^250 ")
			);
		}

		[Fact]
		public async Task StartTlsTest()
		{
			using (var conn = new TestConnection(_output))
			{
				Assert.False(conn.Connection.IsEncrypted, "Is not encrypted to start");
				await conn.FromAsync("");
				await conn.ToAsync("EHLO test.com");
				await conn.FromAsync("250[- ]STARTTLS");
				Assert.False(conn.Connection.IsEncrypted, "Is not encrypted before STARTTLS");
				await conn.ToAsync("STARTTLS");
				Assert.False(conn.Connection.IsEncrypted, "Is not encrypted after STARTTLS, but before negotiation");
				await conn.FromAsync("^220");
				var ssl = new SslStream(
					conn.LocalStream.InnerStream,
					false,
					(sender, certificate, chain, errors) => certificate.Subject == "CN=test.vaettir.net");

				await conn.Do(
					ssl.AuthenticateAsClientAsync(
						"test.vaettir.net",
						null,
						SslProtocols.Tls12,
						false));

				conn.LocalStream.ChangeSteam(ssl);
				Assert.True(conn.Connection.IsEncrypted, "Is encrypted after STARTTLS");
				await conn.ToAsync("EHLO test.com");
				await conn.FromAsync("^((?!STARTTLS).)*$");

				await conn.EndConversation();
			}
		}

		[Fact]
		public async Task WelcomeMessageTest()
		{
			await ConversationTest(
				Txn.From("^220 ")
			);
		}
	}
}
