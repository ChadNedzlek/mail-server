using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server;

namespace MailCore
{
	internal class AgentHandler : CommandHandler
	{
		private readonly ProtocolListener _smtp;
		private readonly MailDispatcher _dispatcher;

		public AgentHandler(
			ProtocolListener smtp,
			MailDispatcher dispatcher)
		{
			_smtp = smtp;
			_dispatcher = dispatcher;
			// var imap
		}

		public override async Task<int> RunAsync(List<string> remaining)
		{
			var cts = new CancellationTokenSource();

			await Task.WhenAll(
				Task.Run(() => _smtp.RunAsync(cts.Token), cts.Token),
				Task.Run(() => _dispatcher.RunAsync(cts.Token), cts.Token)
			);

			return 0;
		}
	}
}