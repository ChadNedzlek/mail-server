using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public class DuplexStream : Stream
	{
		private readonly AutoResetEventAsync _newData = new AutoResetEventAsync(false);
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
		private LinkedList<byte[]> _chunks = new LinkedList<byte[]>();
		private bool _writeClosed;
		public override bool CanRead => _chunks != null;
		public override bool CanSeek => false;
		public override bool CanWrite => !_writeClosed && _chunks != null;

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override void Flush()
		{
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
			return ReadAsync(buffer, offset, count).Result;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			WriteAsync(buffer, offset, count).Wait();
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (_chunks == null)
			{
				throw new ObjectDisposedException(nameof(DuplexStream));
			}

			if (_chunks.Count == 0 && _writeClosed)
			{
				return 0;
			}

			SemaphoreLock semaphoreLock = await SemaphoreLock.GetLockAsync(_semaphore, cancellationToken);
			try
			{
				while (_chunks.Count == 0)
				{
					if (_writeClosed)
					{
						return 0;
					}

					semaphoreLock.Dispose();
					semaphoreLock = null;
					await _newData.WaitAsync(cancellationToken);
					if (_chunks == null)
					{
						throw new ObjectDisposedException("DuplexStream");
					}

					semaphoreLock = await SemaphoreLock.GetLockAsync(_semaphore, cancellationToken);
				}

				cancellationToken.ThrowIfCancellationRequested();

				LinkedListNode<byte[]> node = _chunks.First;
				_chunks.Remove(node);
				byte[] value = node.Value;
				int toCopy = Math.Min(value.Length, count);
				Array.Copy(value, 0, buffer, offset, toCopy);
				if (toCopy != value.Length)
				{
					int remLength = value.Length - toCopy;
					var remainder = new byte[remLength];
					Array.Copy(value, toCopy, remainder, 0, remLength);
					_chunks.AddFirst(remainder);
				}

				return toCopy;
			}
			finally
			{
				semaphoreLock?.Dispose();
			}
		}

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (_writeClosed)
			{
				throw new InvalidOperationException();
			}

			if (_chunks == null)
			{
				throw new ObjectDisposedException(nameof(DuplexStream));
			}

			var insert = new byte[count];
			Array.Copy(buffer, offset, insert, 0, count);
			using (await SemaphoreLock.GetLockAsync(_semaphore, cancellationToken))
			{
				_chunks.AddLast(insert);
			}

			_newData.Set();
		}

		public void CloseWriteChannel()
		{
			_writeClosed = true;
			_newData.Set();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				_writeClosed = true;
				_chunks = null;
				_newData.Set();
			}
		}
	}
}
