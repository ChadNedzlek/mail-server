using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace MailCore
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var serviceCollection = new ServiceCollection();
			var smtpSettings = Settings.Get<SmtpSettings>();
			var smtp = new SmtpListener(smtpSettings)
			{
				MailStore = new FileSystemMailStore(smtpSettings.MailStorePath),
			};
			CancellationTokenSource cts = new CancellationTokenSource();
			var startTask = smtp.Start(cts.Token);
			startTask.GetAwaiter().GetResult();
		}
	}
}
