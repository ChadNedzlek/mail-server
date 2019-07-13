using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Mono.Options;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Imap;
using Vaettir.Mail.Server.Imap.Commands;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Utility;

namespace MailCore
{
	internal class Options
	{
		public string SettingsPath { get; set; } = "mail.config.json";
		public LogLevel Verbosity { get; set; }
	}

	public static class Program
	{
		public static async Task<int> Main(string[] args)
		{
			var o = new Options();
			OptionSet p = new OptionSet
			{
				{"c|config=", "Configuration file. Default 'smtp.config.json'", s => o.SettingsPath = s},
			};

			List<string> remaining;
			try
			{
				remaining = p.Parse(args);
			}
			catch (Exception e)
			{
				ShowHelp(Console.Error, "Usage: ", p, e);
				return 1;
			}

			if (remaining.Count < 1)
			{
				ShowHelp(Console.Error, "Command required", p, null);
				return 1;
			}

			string command = remaining[0];
			remaining.RemoveAt(0);

			using (IContainer container = await BuildContainer(o))
			{
				CommandHandler handler = GetHandler(command, container);
				if (handler == null)
				{
					ShowHelp(Console.Error, $"Unknown command '{command}'", p, null);
					return 1;
				}

				return await handler.RunAsync(remaining);
			}
		}

		private static CommandHandler GetHandler(string command, IContainer container)
		{
			switch (command)
			{
				case "run":
				case "agent":
					return container.ResolveKeyed<CommandHandler>("agent");
				case "user":
					return container.ResolveKeyed<CommandHandler>("user");
				default:
					return null;
			}
		}

		private static void ShowHelp(TextWriter textWriter, string message, OptionSet optionSet, Exception optionException)
		{
			if (message != null)
			{
				textWriter.WriteLine(message);
				textWriter.WriteLine();
			}

			if (optionException != null)
			{
				textWriter.WriteLine($"ERROR: {optionException.Message}");
				textWriter.WriteLine();
			}

			textWriter.WriteLine(
				@"Usage:
  vmail [global options] run
  vmail [global options] user

  global options:
");
			optionSet?.WriteOptionDescriptions(textWriter);
		}

		private static async Task<IContainer> BuildContainer(Options options)
		{
			var builder = new ContainerBuilder();

			CancellationTokenSource abort = new CancellationTokenSource();
			builder.Register(c => abort.Token);

			builder.RegisterInstance(options);
			builder.RegisterType<UserHandler>().Keyed<CommandHandler>("user");
			builder.RegisterType<AgentHandler>().Keyed<CommandHandler>("agent");

			FileWatcherSettings<AgentSettings> settings = FileWatcherSettings<AgentSettings>.Load(options.SettingsPath);
			
			var certificates = ImmutableDictionary.CreateBuilder<string, PrivateKeyHolder>();
			foreach (var connection in settings.Value.Connections)
			{
				if (string.IsNullOrEmpty(connection.Certificate))
					continue;
				certificates.Add(connection.Certificate, await PrivateKeyHolder.LoadAsync(connection.Certificate, abort.Cancel));
			}

			builder.RegisterInstance(new PrivateKeyProvider(certificates.ToImmutable()));

			IDictionary<string, LogSettings> logSettings = settings.Value.Logging;
			if (logSettings != null)
			{
				LogSettings defaultSettings = null;
				string defaultKey = null;
				foreach (KeyValuePair<string, LogSettings> log in logSettings)
				{
					switch (log.Key.ToLowerInvariant())
					{
						case "":
						case "default":
							defaultKey = log.Key;
							defaultSettings = log.Value;
							break;
						case "console":
						case "con":
							builder.RegisterInstance(ChildWatcherSettings.Create(settings, s => s.Logging.GetValueOrDefault(log.Key)))
								.Keyed<IVolatile<LogSettings>>("console");
							builder.RegisterType<ConsoleLogger>()
								.As<ILogSync>();
							break;
					}
				}

				if (defaultSettings != null)
				{
					builder.RegisterInstance(ChildWatcherSettings.Create(settings, s => s.Logging.GetValueOrDefault(defaultKey)))
						.As<IVolatile<LogSettings>>();
				}
			}

			builder.Register(c => new CompositeLogger(c.Resolve<IEnumerable<ILogSync>>())).As<ILogger>();

			builder.RegisterType<DnsClientResolver>()
				.As<IDnsResolve>()
				.SingleInstance();

			builder.RegisterType<WrappedTcpClientProvider>()
				.As<ITcpConnectionProvider>()
				.SingleInstance();

			builder.RegisterType<SmtpSession>()
				.Keyed<IProtocolSession>("smtp")
				.As<ISmtpMessageChannel>()
				.As<IMailBuilder>()
				.InstancePerMatchingLifetimeScope(ProtocolListener.ConnectionScopeTag);

			builder.RegisterType<SmtpAuthenticationTransport>()
				.Keyed<IAuthenticationTransport>("smtp")
				.InstancePerMatchingLifetimeScope(ProtocolListener.ConnectionScopeTag);

			builder.RegisterType<ImapSession>()
				.Keyed<IProtocolSession>("imap")
				.Keyed<IAuthenticationTransport>("imap")
				.As<IImapMessageChannel>()
				.As<IImapMailboxPointer>()
				.InstancePerMatchingLifetimeScope(ProtocolListener.ConnectionScopeTag);

			builder.RegisterType<MailTransfer>();
			builder.RegisterType<MailDispatcher>();

			builder.RegisterType<ProtocolListener>();

			builder.RegisterType<FileSystemMailQueue>().As<IMailQueue>().SingleInstance();
			builder.RegisterType<HashedPasswdUserStore>().As<IUserStore>().SingleInstance();
			builder.RegisterType<FileSystemDomainResolver>().As<IDomainSettingResolver>().SingleInstance();
			builder.RegisterType<FileSystemMailTransferQueue>().As<IMailTransferQueue>();
			builder.RegisterType<FileSystemMailboxStore>().As<IMailboxStore>().As<IMailboxDeliveryStore>();
			builder.RegisterType<FileSystemMailSendFailureManager>().As<IMailSendFailureManager>();
			AgentSettings initialValue = settings.Value;
			builder.RegisterInstance(initialValue)
				.As<AgentSettings>()
				.As<AgentSettings>();
			builder.RegisterInstance(settings)
				.As<IVolatile<AgentSettings>>()
				.As<IVolatile<AgentSettings>>();
			
			builder.RegisterAssemblyTypes(typeof(SmtpSession).GetTypeInfo().Assembly)
				.Where(t => t.GetTypeInfo().GetCustomAttribute<SmtpCommandAttribute>() != null)
				.Keyed<ISmtpCommand>(t => t.GetTypeInfo().GetCustomAttribute<SmtpCommandAttribute>().Name);

			var imapCommands = typeof(ImapSession)
				.Assembly
				.DefinedTypes
				.Select(t => (t, md: t.GetTypeInfo().GetCustomAttribute<ImapCommandAttribute>()))
				.Where(p => p.md != null);
			foreach (var (t, md) in imapCommands)
			{
				builder.RegisterType(t)
					.Keyed<IImapCommand>(md.Name)
					.WithMetadata<ImapCommandMetadata>(m =>
					{
						m.For(x => x.Name, md.Name);
						m.For(x => x.MinimumState, md.MinimumState);
					});
			}

			builder.RegisterType<CapabilityCommand>().As<IImapCommand>();
			
			var authMechs = typeof(IAuthenticationSession)
				.Assembly
				.DefinedTypes
				.Select(t => (t, md: t.GetTypeInfo().GetCustomAttribute<AuthenticationMechanismAttribute>()))
				.Where(p => p.md != null);
			foreach (var (t, md) in authMechs)
			{
				builder.RegisterType(t)
					.WithMetadata<AuthencticationMechanismMetadata>(m =>
					{
						m.For(x => x.Name, md.Name);
						m.For(x => x.RequiresEncryption, md.RequiresEncryption);
					})
					.As<IAuthenticationSession>()
					.Keyed<IAuthenticationSession>(md.Name);
			}

			return builder.Build();
		}
	}
}
