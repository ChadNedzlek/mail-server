using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public static class StreamUtility
	{
		public static async Task<byte[]> ReadExactlyAsync(this Stream stream, int count, CancellationToken cancellationToken)
		{
			var readBuffer = new byte[count];
			var read = 0;
			while (read < count)
			{
				int result = await stream.ReadAsync(readBuffer, read, count - read, cancellationToken);
				if (result == 0)
				{
					return null;
				}
				read += result;
			}
			return readBuffer;
		}

		public static async Task<byte[]> ReadExactlyAsync(
			Func<byte[], int, int, CancellationToken, Task<int>> getBytes,
			int count,
			CancellationToken cancellationToken)
		{
			var readBuffer = new byte[count];
			var read = 0;
			while (read < count)
			{
				int result = await getBytes(readBuffer, read, count - read, cancellationToken);
				if (result == 0)
				{
					return null;
				}
				read += result;
			}
			return readBuffer;
		}
	}
}