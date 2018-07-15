using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public sealed class UnencodedStreamReader
	{
		private readonly Stream _stream;
		private readonly bool _leaveOpen;
		private byte[] _readBuffer = new byte[10000];
		private int _readBufferFilled;
		private int _readBufferUsed;

		public long BytePositition => _stream.Position - _readBufferFilled + _readBufferUsed;

		public UnencodedStreamReader(Stream stream) : this(stream, false)
		{
		}

		public UnencodedStreamReader(Stream stream, bool leaveOpen)
		{
			_stream = stream;
			_leaveOpen = leaveOpen;
		}

		public void Dispose()
		{
			if (!_leaveOpen) _stream?.Dispose();
			_readBuffer = null;
		}

		public async Task<byte[]> ReadLineAsync(CancellationToken cancellationToken)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				byte lastChar = 0;
				ArraySegment<byte> chunk;
				while (!TryFinishLine(ref lastChar, out chunk))
				{
					await stream.WriteAsync(chunk.Array, chunk.Offset, chunk.Count, cancellationToken);
					_readBufferFilled = await ReadBytesAsync(_readBuffer, 0, _readBuffer.Length, cancellationToken);
					if (_readBufferFilled == 0)
					{
						if (stream.Length == 0)
							return null;
						return stream.ToArray();
					}

					_readBufferUsed = 0;
				}

				await stream.WriteAsync(chunk.Array, chunk.Offset, chunk.Count, cancellationToken);
				return stream.ToArray();
			}
		}

		private bool TryFinishLine(ref byte lastChar, out ArraySegment<byte> chunk)
		{
			var charStart = _readBufferUsed;
			var charIndex = charStart;
			while (true)
			{
				if (charIndex == _readBufferFilled)
				{
					_readBufferUsed = charIndex;
					chunk = new ArraySegment<byte>(_readBuffer, charStart, _readBufferFilled - charStart);
					return false;
				}

				if (lastChar == '\r' && _readBuffer[charIndex] == '\n')
				{
					// We found a CRLF set!
					_readBufferUsed = charIndex + 1;
					chunk = new ArraySegment<byte>(_readBuffer, charStart, charIndex - charStart - 1);
					return true;
				}

				lastChar = _readBuffer[charIndex];
				charIndex++;
			}
		}

		public Task<int> ReadBytesAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (_readBufferFilled > _readBufferUsed)
			{
				int toCopy = Math.Min(_readBufferFilled - _readBufferUsed, count);
				Array.Copy(_readBuffer, _readBufferUsed, buffer, offset, toCopy);
				_readBufferUsed += toCopy;
				return Task.FromResult(toCopy);
			}

			return _stream.ReadAsync(buffer, offset, count, cancellationToken);
		}
	}
}
