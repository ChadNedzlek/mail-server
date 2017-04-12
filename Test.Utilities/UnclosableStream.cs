using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Test.Utilities
{
	public class UnclosableStream : Stream
	{
		private readonly Stream _inner;

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return _inner.CopyToAsync(destination, bufferSize, cancellationToken);
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
			return _inner.Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _inner.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override int ReadByte()
		{
			return _inner.ReadByte();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _inner.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_inner.SetLength(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_inner.Write(buffer, offset, count);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _inner.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override void WriteByte(byte value)
		{
			_inner.WriteByte(value);
		}

		public override bool CanRead => _inner.CanRead;

		public override bool CanSeek => _inner.CanSeek;

		public override bool CanTimeout => _inner.CanTimeout;

		public override bool CanWrite => _inner.CanWrite;

		public override long Length => _inner.Length;

		public override long Position
		{
			get => _inner.Position;
			set => _inner.Position = value;
		}

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

		public UnclosableStream(Stream inner)
		{
			_inner = inner;
		}
	}
}