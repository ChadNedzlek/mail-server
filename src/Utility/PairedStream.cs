using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public class PairedStream : Stream
	{
		private readonly Stream _read;
		private readonly Stream _write;

		private PairedStream(Stream read, Stream write)
		{
			_read = read;
			_write = write;
		}

		public override void Flush()
		{
			_read.Flush();
			_write.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _read.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_write.Write(buffer, offset, count);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _write.WriteAsync(buffer, offset, count, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				_read.Dispose();
				_write.Dispose();
			}
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return _read.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.WhenAll(
				_read.FlushAsync(cancellationToken),
				_write.FlushAsync(cancellationToken)
				);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _read.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override int ReadByte()
		{
			return _read.ReadByte();
		}

		public override void WriteByte(byte value)
		{
			_write.WriteByte(value);
		}

		public override bool CanRead => _read.CanRead;
		public override bool CanSeek => false;
		public override bool CanWrite => _write.CanWrite;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public static (Stream a, Stream b) Create()
		{
			DuplexStream a = new DuplexStream();
			DuplexStream b = new DuplexStream();
			return (new PairedStream(a, b), new PairedStream(b, a));
		}
	}
}