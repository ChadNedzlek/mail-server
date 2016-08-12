using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailServer
{
	public sealed class VariableStreamReader : IDisposable
	{
		private readonly Stream _stream;
		private char[] _charBuffer = new char[10000];
		private byte[] _readBuffer = new byte[10000];
		private int _readBufferFilled;
		private int _readBufferUsed;

		public VariableStreamReader(Stream stream)
		{
			_stream = stream;
		}

		public void Dispose()
		{
			_readBuffer = null;
			_charBuffer = null;
		}

		private bool TryFinishLine(Decoder decoder, ref char lastChar, out string chunk)
		{
			var charIndex = 0;
			while (true)
			{
				int bytesUsed;
				int charsUsed;
				bool complete;
				decoder.Convert(
					_readBuffer,
					_readBufferUsed,
					_readBufferFilled - _readBufferUsed,
					_charBuffer,
					charIndex,
					1,
					false,
					out bytesUsed,
					out charsUsed,
					out complete);

				_readBufferUsed += bytesUsed;

				if (charsUsed == 0)
				{
					chunk = new string(_charBuffer, 0, charIndex);
					return false;
				}

				if (lastChar == '\r' && _charBuffer[charIndex] == '\n')
				{
					// We found a CRLF set!
					chunk = new string(_charBuffer, 0, charIndex + 1);
					return true;
				}

				lastChar = _charBuffer[charIndex];
				charIndex++;
			}
		}

		public async Task<string> ReadLineAsync(Encoding encoding, CancellationToken cancellationToken)
		{
			var builder = new StringBuilder();
			string chunk;
			var lastChar = '\0';
			Decoder decoder = encoding.GetDecoder();
			while (!TryFinishLine(decoder, ref lastChar, out chunk))
			{
				builder.Append(chunk);
				_readBufferFilled = await ReadBytesAsync(_readBuffer, 0, _readBuffer.Length, cancellationToken);
				_readBufferUsed = 0;
			}
			builder.Append(chunk);
			builder.Length = builder.Length - 2; // Remove the CR LF
			return builder.ToString();
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