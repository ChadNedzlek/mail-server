using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;
using Xunit;

namespace Tests
{
	public class MutexThreadTests
	{
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
				Assert.InRange(result.TimeTaken.TotalSeconds, 1, 10000);
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
				Assert.InRange(result.TimeTaken.TotalSeconds, 0, 1);
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
				await Task.Delay(TimeSpan.FromSeconds(2));
				waiter.Release(m);
				var result = await childTask;
				Assert.True(result.Waited, "Other thread should have waited");
				Assert.InRange(result.TimeTaken.TotalSeconds, 1 , 5);
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
			bool waited = mutex.WaitOne(TimeSpan.FromSeconds(secondsToWait));
			if (waited)
			{
				mutex.ReleaseMutex();
			}

			watch.Stop();
			return new WaitResult(watch.Elapsed, waited);
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