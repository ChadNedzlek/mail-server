using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public class BoundedStream : Stream
	{
		public override long Length { get; }

		private readonly long _start;
		private readonly Stream _inner;

		public BoundedStream(Stream inner, long length)
		{
			_start = inner.Position;
			Length = length;
			_inner = inner;
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			count = (int) Math.Min(count, Length - Position);
			return _inner.BeginRead(buffer, offset, count, callback, state);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			throw new NotSupportedException();
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			var read = _inner.EndRead(asyncResult);
			Position += read;
			return read;
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException();
		}

		public override void Flush()
		{
			_inner.Flush();
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return _inner.FlushAsync(cancellationToken);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			count = (int) Math.Min(count, Length - Position);
			var read =_inner.Read(buffer, offset, count);
			Position += read;
			return read;
		}

		public override int Read(Span<byte> buffer)
		{
			if (buffer.Length > Length - Position)
			{
				buffer = buffer.Slice(0, (int) (Length - Position));
			}

			var read =_inner.Read(buffer);
			Position += read;
			return read;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			count = (int) Math.Min(count, Length - Position);
			var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken);
			Position += read;
			return read;
		}

		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
		{
			if (buffer.Length > Length - Position)
			{
				buffer = buffer.Slice(0, (int) (Length - Position));
			}

			var read = await _inner.ReadAsync(buffer, cancellationToken);
			Position += read;
			return read;
		}

		public override int ReadByte()
		{
			if (Position >= Length)
				return -1;
			Position++;
			return _inner.ReadByte();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					if (offset < 0 || offset > Length)
					{
						throw new ArgumentOutOfRangeException();
					}

					return _inner.Seek(offset + _start, origin) - _start;
				case SeekOrigin.Current:
					long newPosition = Position + offset;
					if (newPosition < _start || newPosition > _start + Length)
					{
						throw new ArgumentOutOfRangeException();
					}
					return _inner.Seek(offset + _start, origin) - _start;
				case SeekOrigin.End:
					return Seek(_start + Length + offset, SeekOrigin.Begin);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new NotSupportedException();
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
		{
			throw new NotSupportedException();
		}

		public override void WriteByte(byte value)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead => _inner.CanRead;
		public override bool CanSeek => _inner.CanSeek;
		public override bool CanTimeout => _inner.CanTimeout;
		public override bool CanWrite => false;
		public override long Position { get; set; }

		public override int ReadTimeout
		{
			get => _inner.ReadTimeout;
			set => _inner.ReadTimeout = value;
		}

		public override int WriteTimeout 
		{
			get => _inner.WriteTimeout;
			set => _inner.WriteTimeout = value;
		}
	}
}
