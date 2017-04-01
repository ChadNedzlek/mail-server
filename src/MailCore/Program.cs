using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Mail.Server.Authentication.Mechanism;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Mail.Server.Smtp.Commands;
using Vaettir.Utility;

namespace MailCore
{
	public class Program
	{
		public static void Main(string[] args)
		{
			using (IContainer container = BuildContainer())
			{
				var smtp = container.Resolve<ProtocolListener>();
				var cts = new CancellationTokenSource();
				Task startTask = smtp.RunAsync(cts.Token);
				startTask.GetAwaiter().GetResult();
			}
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

			builder.RegisterType<ProtocolListener>();
			builder.RegisterType<FileSystemMailQueue>().As<IMailQueue>().SingleInstance();
			builder.RegisterType<HashedPasswordUserStore>().As<IUserStore>().SingleInstance();
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