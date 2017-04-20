using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public class MutexThread
	{
		private readonly ConcurrentBag<WaitBlock> _pending = new ConcurrentBag<WaitBlock>();
		private readonly EventWaitHandle _pendingReady = new AutoResetEvent(false);
		private readonly ConcurrentBag<Mutex> _releaseable = new ConcurrentBag<Mutex>();

		private MutexThread()
		{
		}

		public Task Task { get; private set; }

		private void Run(CancellationToken token)
		{
			using (token.Register(() => _pendingReady.Set()))
			{
				var waiting = new List<WaitBlock>();
				while (!token.IsCancellationRequested)
				{
					var waits = new WaitHandle[1 + waiting.Count];
					waits[0] = _pendingReady;
					waiting.Select(w => (WaitHandle) w.Mutex).ToList().CopyTo(waits, 1);

					int index = WaitHandle.WaitAny(waits, TimeSpan.FromSeconds(1));
					{
						Mutex toRelease;
						while (_releaseable.TryTake(out toRelease))
						{
							toRelease.ReleaseMutex();
						}
					}

					if (index == WaitHandle.WaitTimeout)
					{
						continue;
					}

					if (index == 0)
					{
						WaitBlock item;
						while (_pending.TryTake(out item))
						{
							waiting.Add(item);
						}
						continue;
					}

					index--;

					{
						WaitBlock item = waiting[index];
						waiting.RemoveAt(index);
						item.Source.TrySetResult(null);
					}
				}
			}
		}

		public Task<IDisposable> WaitAsync(Mutex mutex)
		{
			var block = new WaitBlock(mutex);
			_pending.Add(block);
			_pendingReady.Set();
			return block.Source.Task.ContinueWith(_ => (IDisposable) new MutexLock(this, mutex));
		}

		public void Release(Mutex mutex)
		{
			_releaseable.Add(mutex);
			_pendingReady.Set();
		}

		public static MutexThread Begin(CancellationToken token)
		{
			var thread = new MutexThread();
			thread.Task = Task.Run(() => thread.Run(token), token);
			return thread;
		}

		private class WaitBlock
		{
			public WaitBlock(Mutex mutex)
			{
				Mutex = mutex;
				Source = new TaskCompletionSource<object>();
			}

			public Mutex Mutex { get; }

			public TaskCompletionSource<object> Source { get; }
		}

		private sealed class MutexLock : IDisposable
		{
			private Mutex _mutex;
			private MutexThread _mutexThread;

			public MutexLock(MutexThread mutexThread, Mutex mutex)
			{
				_mutexThread = mutexThread;
				_mutex = mutex;
			}

			public void Dispose()
			{
				Interlocked.Exchange(ref _mutexThread, null)?.Release(_mutex);
				_mutex = null;
			}
		}
	}
}
