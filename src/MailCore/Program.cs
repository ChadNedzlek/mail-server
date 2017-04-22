using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Utility;

namespace MailCore
{
	public static class Program
	{
		[UsedImplicitly]
		public static int Main(string[] args)
		{
			return MailAsync().GetAwaiter().GetResult();
		}

		private static async Task<int> MailAsync()
		{
			using (IContainer container = BuildContainer())
			{
				var smtp = container.Resolve<ProtocolListener>();
				var dispatcher = container.Resolve<MailDispatcher>();
				// var imap
				var cts = new CancellationTokenSource();

				await Task.WhenAll(
					Task.Run(() => smtp.RunAsync(cts.Token), cts.Token),
					Task.Run(() => dispatcher.RunAsync(cts.Token), cts.Token)
					);
			}
			return 0;
		}

		private static IContainer BuildContainer()
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

			FileWatcherSettings<SmtpSettings> settings = FileWatcherSettings<SmtpSettings>.Load("smtp.config.json");
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
