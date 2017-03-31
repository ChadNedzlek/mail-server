using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
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
		        CancellationTokenSource cts = new CancellationTokenSource();
		        var startTask = smtp.RunAsync(cts.Token);
		        startTask.GetAwaiter().GetResult();
	        }
        }

        private static IContainer BuildContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterInstance(new CompositeLogger(new[] { new ConsoleLogger() }))
                .As<ILogger>();

            builder.RegisterType<SmtpSession>()
                .As<IProtocolSession>()
                .As<IMessageChannel>()
                .As<IAuthenticationTransport>()
                .As<IMailBuilder>()
                .InstancePerLifetimeScope();

            builder.RegisterType<ProtocolListener>();
            builder.RegisterType<FileSystemMailStore>().As<IMailStore>().SingleInstance();
            builder.RegisterType<HashedPasswordUserStore>().As<IUserStore>().SingleInstance();
            builder.RegisterInstance(Settings.Get<SmtpSettings>())
                .As<SmtpSettings>()
                .As<ProtocolSettings>();

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
