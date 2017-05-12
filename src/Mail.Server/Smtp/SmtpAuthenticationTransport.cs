using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Server.Smtp
{
	[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	public class SmtpAuthenticationTransport : IAuthenticationTransport
	{
		private readonly ISmtpMessageChannel _channel;
		private readonly IVariableStreamReader _reader;

		public SmtpAuthenticationTransport(
			ISmtpMessageChannel channel,
			IVariableStreamReader reader)
		{
			_channel = channel;
			_reader = reader;
		}

		public Task SendAuthenticationFragmentAsync(byte[] data, CancellationToken cancellationToken)
		{
			return _channel.SendReplyAsync(SmtpReplyCode.AuthenticationFragment, Convert.ToBase64String(data), cancellationToken);
		}

		public async Task<byte[]> ReadAuthenticationFragmentAsync(CancellationToken cancellationToken)
		{
			return Convert.FromBase64String(await _reader.ReadLineAsync(Encoding.ASCII, cancellationToken));
		}
	}
}