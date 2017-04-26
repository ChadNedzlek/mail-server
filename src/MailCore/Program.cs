using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Mono.Options;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Utility;

namespace MailCore
{
	internal class Options
	{
		public string SettingsPath { get; set; } = "smtp.config.json";
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

			CommandHandler handler = GetHandler(command);
			if (handler == null)
			{
				ShowHelp(Console.Error, $"Unknown command '{command}'", p, null);
				return 1;
			}

			using (var container = BuildContainer(o))
			{
				return handler.RunAsync(container, o, remaining).GetAwaiter().GetResult();
			}
		}

		private static CommandHandler GetHandler(string command)
		{
			switch (command)
			{
				case "run":
					return new AgentHandler();
				case "user":
					return new UserHandler();
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

		internal static IContainer BuildContainer(Options options)
		{
			var builder = new ContainerBuilder();

			builder.RegisterInstance(new CompositeLogger(new[] {new ConsoleLogger()}))
				.As<ILogger>();

			builder.RegisterType<SmtpSession>()
				.As<IProtocolSession>()
				.As<IMessageChannel>()
				.As<IAuthenticationTransport>()
				.As<IMailBuilder>()
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

			FileWatcherSettings<SmtpSettings> settings = FileWatcherSettings<SmtpSettings>.Load(options.SettingsPath);
			SmtpSettings initialValue = settings.Value;
			builder.RegisterInstance(initialValue)
				.As<SmtpSettings>()
				.As<ProtocolSettings>();
			builder.RegisterInstance(settings)
				.As<IVolatile<SmtpSettings>>()
				.As<IVolatile<ProtocolSettings>>();

			builder.RegisterAssemblyTypes(typeof(SmtpSession).GetTypeInfo().Assembly)
				.Where(t => t.GetTypeInfo().GetCustomAttribute<CommandAttribute>() != null)
				.Keyed<ICommand>(t => t.GetTypeInfo().GetCustomAttribute<CommandAttribute>().Name);

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
