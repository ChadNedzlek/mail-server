using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public sealed class UnencodedStreamReader : IDisposable
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

		public enum ReadState
		{
			More,
			EndOfStream,
			InputBufferTooSmall,
		}

		public async Task<int?> TryReadLineAsync(Memory<byte> output, CancellationToken cancellationToken)
		{
			byte lastChar = 0;

			int readCount = 0;
			int read;
			while (!TryFinishLine(ref lastChar, output.Span, out read))
			{
				output = output.Slice(read);
				readCount += read;
				_readBufferFilled = await ReadBytesAsync(_readBuffer, 0, _readBuffer.Length, cancellationToken);
				if (_readBufferFilled == 0)
				{
					return readCount == 0 ? (int?) null : readCount;
				}
			}
			readCount += read;
			return readCount;
		}

		private bool TryFinishLine(ref byte lastChar, in Span<byte> target, out int readBytes)
		{
			var charStart = _readBufferUsed;
			var charIndex = charStart;
			while (true)
			{
				if (charIndex == _readBufferFilled)
				{
					_readBufferUsed = charIndex;
					readBytes = _readBufferFilled - charStart;
					_readBuffer.AsSpan(charStart, readBytes).CopyTo(target);
					return false;
				}

				if (lastChar == '\r' && _readBuffer[charIndex] == '\n')
				{
					// We found a CRLF set!
					_readBufferUsed = charIndex + 1;
					readBytes = charIndex - charStart - 1;
					_readBuffer.AsSpan(charStart, readBytes).CopyTo(target);
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

			_readBufferUsed = 0;
			return _stream.ReadAsync(buffer, offset, count, cancellationToken);
		}
	}
}
