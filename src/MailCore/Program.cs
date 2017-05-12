using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using JetBrains.Annotations;
using Mono.Options;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Imap;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Utility;

namespace MailCore
{
	internal class Options
	{
		public string SettingsPath { get; set; } = "smtp.config.json";
		public LogLevel Verbosity { get; set; }
	}

	public static class Program
	{
		[UsedImplicitly]
		public static int Main(string[] args)
		{
			Options o = new Options();
			OptionSet p = new OptionSet()
				.Add("config|c=", "Configuration file. Default 'smtp.config.json'", s => o.SettingsPath = s);

			List<string> remaining = null;
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

			var command = remaining[0];
			remaining.RemoveAt(0);

			using (var container = BuildContainer(o))
			{
				CommandHandler handler = GetHandler(command, container);
				if (handler == null)
				{
					ShowHelp(Console.Error, $"Unknown command '{command}'", p, null);
					return 1;
				}
				return handler.RunAsync(remaining).GetAwaiter().GetResult();
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

		internal static void ShowHelp(TextWriter textWriter, string message, OptionSet optionSet, Exception optionException)
		{
			if (message != null)
			{
				textWriter.WriteLine(message);
				textWriter.WriteLine();
			}
			textWriter.WriteLine(
				@"Usage:
  vmail [global options] run

  global options:
");
			optionSet?.WriteOptionDescriptions(textWriter);
		}

		private static IContainer BuildContainer(Options options)
		{
			var builder = new ContainerBuilder();

			builder.RegisterInstance(options);
			builder.RegisterType<UserHandler>().Keyed<CommandHandler>("user");
			builder.RegisterType<AgentHandler>().Keyed<CommandHandler>("agent");

			FileWatcherSettings<AgentSettings> settings = FileWatcherSettings<AgentSettings>.Load(options.SettingsPath);

			IDictionary<string, LogSettings> logSettings = settings.Value.Logging;
			if (logSettings != null)
			{
				LogSettings defaultSettings = null;
				string defaultKey = null;
				foreach (var log in logSettings)
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

			builder.RegisterType<SmtpSession>()
				.Keyed<IProtocolSession>("smtp")
				.As<ISmtpMessageChannel>()
				.As<IMailBuilder>()
				.InstancePerLifetimeScope();

			builder.RegisterType<SmtpAuthenticationTransport>()
				.Keyed<IAuthenticationTransport>("smtp")
				.InstancePerLifetimeScope();

			builder.RegisterType<ImapSession>()
				.Keyed<IProtocolSession>("imap")
				.Keyed<IAuthenticationTransport>("imap")
				.As<IImapMessageChannel>()
				.As<IImapMailboxPointer>()
				.InstancePerLifetimeScope();

			builder.RegisterType<MailTransfer>();
			builder.RegisterType<MailDispatcher>();

			builder.RegisterType<ProtocolListener>();

			builder.RegisterType<FileSystemMailQueue>().As<IMailQueue>().SingleInstance();
			builder.RegisterType<HashedPasswordUserStore>().As<IUserStore>().SingleInstance();
			builder.RegisterType<FileSystemDomainResolver>().As<IDomainSettingResolver>().SingleInstance();
			builder.RegisterType<FileSystemMailTransferQueue>().As<IMailTransferQueue>();
			builder.RegisterType<FileSystemMailboxStore>().As<IMailboxStore>();
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

			builder.RegisterAssemblyTypes(typeof(IAuthenticationSession).GetTypeInfo().Assembly)
				.Where(t => t.GetTypeInfo().GetCustomAttribute<AuthenticationMechanismAttribute>() != null)
				.Keyed<IAuthenticationSession>(
					t => t.GetTypeInfo().GetCustomAttribute<AuthenticationMechanismAttribute>().Name)
				.WithMetadata(
					"RequiresEncryption",
					t => t.GetTypeInfo().GetCustomAttribute<AuthenticationMechanismAttribute>().RequiresEncryption);

			return builder.Build();
		}
	}
}
