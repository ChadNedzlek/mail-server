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
		private readonly CancellationToken _cancellationToken;

		public AgentHandler(
			ProtocolListener protocol,
			MailDispatcher dispatcher,
			MailTransfer transfer,
			CancellationToken cancellationToken)
		{
			_protocol = protocol;
			_dispatcher = dispatcher;
			_transfer = transfer;
			_cancellationToken = cancellationToken;
		}

		public override async Task<int> RunAsync(List<string> remaining)
		{
			await Task.WhenAll(
				Task.Run(() => _protocol.RunAsync(_cancellationToken), _cancellationToken),
				Task.Run(() => _dispatcher.RunAsync(_cancellationToken), _cancellationToken),
				Task.Run(() => _transfer.RunAsync(_cancellationToken), _cancellationToken)
			);

			return 0;
		}
	}
}
