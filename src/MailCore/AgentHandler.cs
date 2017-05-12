using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server;

namespace MailCore
{
	internal class AgentHandler : CommandHandler
	{
		private readonly ProtocolListener _protocol;
		private readonly MailDispatcher _dispatcher;
		private readonly MailTransfer _transfer;

		public AgentHandler(
			ProtocolListener protocol,
			MailDispatcher dispatcher,
			MailTransfer transfer)
		{
			_protocol = protocol;
			_dispatcher = dispatcher;
			_transfer = transfer;
		}

		public override async Task<int> RunAsync(List<string> remaining)
		{
			var cts = new CancellationTokenSource();

			await Task.WhenAll(
				Task.Run(() => _protocol.RunAsync(cts.Token), cts.Token),
				Task.Run(() => _dispatcher.RunAsync(cts.Token), cts.Token),
				Task.Run(() => _transfer.RunAsync(cts.Token), cts.Token)
			);

			return 0;
		}
	}
}