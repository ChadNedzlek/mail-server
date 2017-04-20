using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public sealed class RedirectableStream : Stream
	{
		private Stream _innerStream;

		public RedirectableStream(Stream innerStream)
		{
			if (innerStream == null)
			{
				throw new ArgumentNullException(nameof(innerStream));
			}

			_innerStream = innerStream;
		}

		public override bool CanTimeout { get; }
		public override int ReadTimeout { get; set; }
		public override int WriteTimeout { get; set; }
		public override bool CanRead => _innerStream.CanRead;
		public override bool CanSeek => _innerStream.CanSeek;
		public override bool CanWrite => _innerStream.CanWrite;
		public override long Length => _innerStream.Length;

		public override long Position
		{
			get => _innerStream.Position;
			set => _innerStream.Position = value;
		}

		public Stream InnerStream => _innerStream;

		public Stream ChangeSteam(Stream newStream)
		{
			if (newStream == null)
			{
				throw new ArgumentNullException(nameof(newStream));
			}

			return Interlocked.Exchange(ref _innerStream, newStream);
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_innerStream.Dispose();
			}

			base.Dispose(disposing);
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return _innerStream.FlushAsync(cancellationToken);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public override int ReadByte()
		{
			return _innerStream.ReadByte();
		}

		public override void WriteByte(byte value)
		{
			_innerStream.WriteByte(value);
		}

		public override void Flush()
		{
			_innerStream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _innerStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_innerStream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return _innerStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_innerStream.Write(buffer, offset, count);
		}
	}
}
