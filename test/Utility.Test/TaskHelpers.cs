using System;
using System.Threading.Tasks;
using Xunit;

namespace Utility.Test
{
    public static class TaskHelpers
    {
        public static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(1);

        public static async Task AssertTriggered(Task t)
        {
            var completed = await Task.WhenAny(t, Task.Delay(WaitTime));
            Assert.Same(t, completed);
        }

        public static async Task AssertNotTriggered(Task t)
        {
            var completed = await Task.WhenAny(t, Task.Delay(WaitTime));
            Assert.NotSame(t, completed);
        }

        public static async Task AssertCancelled(Task t)
        {
            bool cancelled;
            try
			{
			    Task task = await Task.WhenAny(t, Task.Delay(WaitTime));
			    if (task == t)
			    {
			        await task;
					cancelled = true;
				}
			    else
			    {
			        cancelled = false;
			    }
			}
            catch (TaskCanceledException)
            {
                cancelled = true;
            }
            Assert.True(cancelled, "Task was cancelled");
        }
    }
}