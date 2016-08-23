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

		private MutexThread()
		{
		}

		private readonly ConcurrentBag<WaitBlock> _pending = new ConcurrentBag<WaitBlock>();
		private readonly ConcurrentBag<Mutex> _releaseable = new ConcurrentBag<Mutex>();
		private readonly EventWaitHandle _pendingReady = new AutoResetEvent(false);

		private void Run(CancellationToken token)
		{
			List<WaitBlock> waiting = new List<WaitBlock>();
			while (!token.IsCancellationRequested)
			{
				var waits = new WaitHandle[1 + waiting.Count];
				waits[0] = _pendingReady;
				waiting.Select(w => (WaitHandle)w.Mutex).ToList().CopyTo(waits, 1);

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
					var item = waiting[index];
					waiting.RemoveAt(index);
					item.Source.TrySetResult(null);
				}
			}
		}

		public Task WaitAsync(Mutex mutex)
		{
			WaitBlock block = new WaitBlock(mutex);
			_pending.Add(block);
			_pendingReady.Set();
			return block.Source.Task;
		}

		public void Release(Mutex mutex)
		{
			_releaseable.Add(mutex);
			_pendingReady.Set();
		}

		public static MutexThread Begin(CancellationToken token, out Task complete)
		{
			var thread = new MutexThread();
			complete = Task.Run(() => thread.Run(token), token);
			return thread;
		}
	}
}