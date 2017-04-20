using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Test.Utilities;
using Vaettir.Utility;
using Xunit;

namespace Utility.Test
{
	public class AutoResetEventAsyncTests
	{
		[Fact]
		public async Task Cancellable()
		{
			var e = new AutoResetEventAsync(false);
			var src = new CancellationTokenSource();
			await TaskHelpers.AssertNotTriggered(e.WaitAsync(src.Token));
		}

		[Fact]
		public async Task CancellableTrigger()
		{
			var e = new AutoResetEventAsync(false);
			var src = new CancellationTokenSource();
			e.Set();
			await TaskHelpers.AssertTriggered(e.WaitAsync(src.Token));
		}

		[Fact]
		public async Task DelayCancelled()
		{
			var e = new AutoResetEventAsync(false);
			var src = new CancellationTokenSource();
			Task waitAsync = e.WaitAsync(src.Token);
			src.CancelAfter(TimeSpan.FromSeconds(TaskHelpers.WaitTime.TotalSeconds / 2));
			await TaskHelpers.AssertCancelled(waitAsync);
		}

		[Fact]
		public async Task DelayTriggable()
		{
			var e = new AutoResetEventAsync(false);
			Task wait = e.WaitAsync();
			e.Set();
			await TaskHelpers.AssertTriggered(wait);
		}

		[Fact]
		public async Task DoubleCanncelled()
		{
			var e = new AutoResetEventAsync(false);
			var src = new CancellationTokenSource();
			Task waitAsync = e.WaitAsync(src.Token);
			Task wait2Async = e.WaitAsync(src.Token);
			src.Cancel();
			await TaskHelpers.AssertCancelled(waitAsync);
			await TaskHelpers.AssertCancelled(wait2Async);
		}

		[Fact]
		public async Task DoubleTriggered()
		{
			var e = new AutoResetEventAsync(false);
			Task waitAsync = e.WaitAsync();
			Task wait2Async = e.WaitAsync();
			e.Set();
			await TaskHelpers.AssertTriggered(waitAsync);
			await TaskHelpers.AssertNotTriggered(wait2Async);
		}

		[Fact]
		public async Task NoDoubleTrigger()
		{
			var e = new AutoResetEventAsync(false);
			e.Set();
			await TaskHelpers.AssertTriggered(e.WaitAsync());
			await TaskHelpers.AssertNotTriggered(e.WaitAsync());
		}

		[Fact]
		public async Task PostCancelled()
		{
			var e = new AutoResetEventAsync(false);
			var src = new CancellationTokenSource();
			Task waitAsync = e.WaitAsync(src.Token);
			src.Cancel();
			await TaskHelpers.AssertCancelled(waitAsync);
		}

		[Fact]
		public async Task Precancelled()
		{
			var e = new AutoResetEventAsync(false);
			var src = new CancellationTokenSource();
			src.Cancel();
			await TaskHelpers.AssertCancelled(e.WaitAsync(src.Token));
		}

		[Fact]
		public async Task Triggable()
		{
			var e = new AutoResetEventAsync(false);
			e.Set();
			await TaskHelpers.AssertTriggered(e.WaitAsync());
		}

		[Fact]
		public async Task Triggered()
		{
			var e = new AutoResetEventAsync(true);
			await TaskHelpers.AssertTriggered(e.WaitAsync());
		}

		[Fact]
		public async Task Uncancellable()
		{
			var e = new AutoResetEventAsync(false);
			await TaskHelpers.AssertNotTriggered(e.WaitAsync(CancellationToken.None));
		}

		[Fact]
		public async Task UncancellableTrigger()
		{
			var e = new AutoResetEventAsync(false);
			e.Set();
			await TaskHelpers.AssertTriggered(e.WaitAsync(CancellationToken.None));
		}

		[Fact]
		public async Task UnTriggered()
		{
			var e = new AutoResetEventAsync(false);
			await TaskHelpers.AssertNotTriggered(e.WaitAsync());
		}
	}
}
