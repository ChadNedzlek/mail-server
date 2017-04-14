using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public static class TaskUtil
	{
		public static Task<TOut> Cast<TIn, TOut>(this Task<TIn> task)
		{
			return task.ContinueWith(t => (TOut)(object)t.Result, TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
		}
	}
}