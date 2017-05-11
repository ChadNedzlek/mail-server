using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IVariableStreamReader : IDisposable
	{
		Task<string> ReadLineAsync(Encoding encoding, CancellationToken cancellationToken);
		Task<int> ReadBytesAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
	}
}