using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace MailCore
{
	[Injected]
	internal class AgentHandler : CommandHandler
	{
		private readonly MailDispatcher _dispatcher;
		private readonly ProtocolListener _protocol;
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
