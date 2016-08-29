﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;
using Xunit;

namespace Utility.Test
{
	public class MutexThreadTests
	{
		private readonly TimeSpan _step = TimeSpan.FromMilliseconds(10);

		[Fact]
		public async Task IsAbortable()
		{
			CancellationTokenSource source = new CancellationTokenSource();
			MutexThread waiter = MutexThread.Begin(source.Token);
			Assert.False(waiter.Task.IsCompleted);
			source.Cancel();
			await waiter.Task;
		}

		[Fact]
		public async Task NoPrematureReturn()
		{
			MutexThread waiter = MutexThread.Begin(CancellationToken.None);
			Mutex m = new Mutex();
			try
			{
				await waiter.WaitAsync(m);
				var result = await Task.Run(() => Wait(m, 2));
				Assert.False(result.Waited, "Other thread did not wait");
				Assert.InRange(result.TimeTaken, _step, MultTime(_step, 10));
			}
			finally
			{
				waiter.Release(m);
			}
		}

		[Fact]
		public async Task SignalReturns()
		{
			MutexThread waiter = MutexThread.Begin(CancellationToken.None);
			Mutex m = new Mutex();
			try
			{
				await waiter.WaitAsync(m);
				Task<WaitResult> childTask = Task.Run(() => Wait(m, 2));
				waiter.Release(m);
				var result = await childTask;
				Assert.True(result.Waited, "Other thread should have waited");
				Assert.InRange(result.TimeTaken, TimeSpan.Zero, _step);
			}
			finally
			{
				waiter.Release(m);
			}
		}

		[Fact]
		public async Task SignalReturnsOnTime()
		{
			MutexThread waiter = MutexThread.Begin(CancellationToken.None);
			Mutex m = new Mutex();
			try
			{
				await waiter.WaitAsync(m);
				Task<WaitResult> childTask = Task.Run(() => Wait(m, 5));
				await Task.Delay(MultTime(_step, 2));
				waiter.Release(m);
				var result = await childTask;
				Assert.True(result.Waited, "Other thread should have waited");
				Assert.InRange(result.TimeTaken, _step, MultTime(_step, 5));
			}
			finally
			{
				waiter.Release(m);
			}
		}

		private WaitResult Wait(Mutex mutex, int secondsToWait)
		{
			Stopwatch watch = new Stopwatch();
			watch.Start();
			bool waited = mutex.WaitOne(MultTime(_step, secondsToWait));
			if (waited)
			{
				mutex.ReleaseMutex();
			}

			watch.Stop();
			return new WaitResult(watch.Elapsed, waited);
		}

		private TimeSpan MultTime(TimeSpan ts, double mult)
		{
			return new TimeSpan((long) (ts.Ticks * mult));
		}

		private struct WaitResult
		{
			public WaitResult(TimeSpan timeTaken, bool waited)
			{
				TimeTaken = timeTaken;
				Waited = waited;
			}

			public TimeSpan TimeTaken { get; }
			public bool Waited { get; }
		}
	}
}