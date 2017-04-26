using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server;

namespace MailCore
{
	internal class AgentHandler : CommandHandler
	{
		public override async Task<int> RunAsync(IContainer container, Options options, List<string> remaining)
		{
			var smtp = container.Resolve<ProtocolListener>();
			var dispatcher = container.Resolve<MailDispatcher>();
			// var imap
			var cts = new CancellationTokenSource();

			await Task.WhenAll(
				Task.Run(() => smtp.RunAsync(cts.Token), cts.Token),
				Task.Run(() => dispatcher.RunAsync(cts.Token), cts.Token)
			);

			return 0;
		}
	}
}