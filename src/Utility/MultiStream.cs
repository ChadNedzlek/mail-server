using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public class MultiStream : Stream
	{
		private readonly ImmutableList<Stream> _streams;
		private readonly bool _leaveOpen;

		public MultiStream(params Stream[] streams)
			: this(streams, false)
		{
		}

		public MultiStream(ImmutableList<Stream> streams)
			: this(streams, false)
		{
		}

		public MultiStream(IEnumerable<Stream> streams)
			: this(streams.ToArray())
		{
		}

		public MultiStream(Stream[] streams, bool leaveOpen)
		{
			if (streams == null) throw new ArgumentNullException(nameof(streams));

			_streams = ImmutableList.Create(streams);
			_leaveOpen = leaveOpen;
		}

		public MultiStream(ImmutableList<Stream> streams, bool leaveOpen)
		{
			if (streams == null) throw new ArgumentNullException(nameof(streams));

			_streams = streams;
			_leaveOpen = leaveOpen;
		}

		public MultiStream(IEnumerable<Stream> streams, bool leaveOpen)
			: this(streams?.ToArray(), leaveOpen)
		{
		}

		private void Do(Action<Stream> action)
		{
			foreach (var stream in _streams)
			{
				action(stream);
			}
		}

		private IEnumerable<T> Do<T>(Func<Stream, T> action)
		{
			return _streams.Select(action);
		}

		public override void Flush()
		{
			Do(s => s.Flush());
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return Task.WhenAll(Do(s => s.FlushAsync(cancellationToken)));
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (!CanRead) throw new InvalidOperationException();
			return _streams[0].Read(buffer, offset, count);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!CanRead) throw new InvalidOperationException();
			return _streams[0].ReadAsync(buffer, offset, count, cancellationToken);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return Do(s => s.Seek(offset, origin)).ToList()[0];
		}

		public override void SetLength(long value)
		{
			Do(s => s.SetLength(value));
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Do(s => s.Write(buffer, offset, count));
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.WhenAll(Do(s => s.WriteAsync(buffer, offset, count, cancellationToken)));
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			return Task.WhenAll(Do(s => s.CopyToAsync(destination, bufferSize, cancellationToken)));
		}

		public override bool CanRead => _streams.Count == 1 && _streams[0].CanRead;

		public override bool CanSeek => _streams.All(s => s.CanSeek);

		public override bool CanWrite => _streams.All(s => s.CanWrite);

		public override long Length
		{
			get
			{
				if (!CanRead) throw new InvalidOperationException();
			    return _streams[0].Length;
			}
		}

		public override long Position
		{
			get
			{
				if (!CanRead) throw new InvalidOperationException();
				return _streams[0].Position;
			}
		    set
		    {
		        foreach (var s  in _streams)
		        {
		            s.Position = value;
		        }
		    }
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !_leaveOpen)
			{
				Do(s => s.Dispose());
			}

			base.Dispose(disposing);
		}
	}
}