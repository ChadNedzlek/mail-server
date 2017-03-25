using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using MailServer;
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
	        ContainerBuilder builder = new ContainerBuilder();

	        builder.RegisterType<SmtpSession>().As<IProtocolSession>().InstancePerLifetimeScope();
	        builder.RegisterGeneratedFactory<SessionFactory>().InstancePerLifetimeScope();
	        builder.RegisterType<ProtocolListener>();
	        builder.RegisterType<FileSystemMailStore>().As<IMailStore>().SingleInstance();
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

			using (IContainer container = builder.Build())
	        {
		        var smtp = container.Resolve<ProtocolListener>();
		        CancellationTokenSource cts = new CancellationTokenSource();
		        var startTask = smtp.RunAsync(cts.Token);
		        startTask.GetAwaiter().GetResult();
	        }
        }
    }
}
