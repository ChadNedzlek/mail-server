using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.FileSystem;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace MailCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        var smtpSettings = Settings.Get<SmtpSettings>();
	        var smtp = new SmtpListener(smtpSettings) {
				MailStore = new FileSystemMailStore(smtpSettings.MailStorePath),
			};
	        CancellationTokenSource cts = new CancellationTokenSource();
	        var startTask = smtp.Start(cts.Token);
	        startTask.GetAwaiter().GetResult();
        }
    }
}
