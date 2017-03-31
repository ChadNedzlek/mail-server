using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
    public static class LocalExtensions
	{
	    public static IEnumerable<T> Append<T>(this IEnumerable<T> list, T item)
	    {
	        return list.Concat(new[] {item});
	    }

	    public static Task<bool> TryReadLineAsync(this TextReader reader, Action<string> getLine, CancellationToken token)
		{
			return reader.ReadLineAsync().WithCancellation(token).ContinueWith(
				l =>
				{
					string line = l.Result;
					getLine(line);
					return line != null;
				}, token);
		}

	    public static bool TryReadLine(this TextReader reader, out string line)
	    {
	        line = reader.ReadLine();
	        return line != null;
		}

		public static Task WithCancellation(this Task task, CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return task;

			return WithCancellationImpl(task, token);
		}

		private static async Task WithCancellationImpl(Task task, CancellationToken token)
		{
		    var tcs = new TaskCompletionSource<bool>();
			using (token.Register((t) => ((TaskCompletionSource<bool>)t).SetCanceled(), tcs))
			{
		        await await Task.WhenAny(task, tcs.Task);
		    }
		}

		public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return task;

			return WithCancellationImpl(task, token);
		}

	    private static async Task<T> WithCancellationImpl<T>(Task<T> task, CancellationToken token)
	    {
	        var tcs = new TaskCompletionSource<bool>();
	        using (token.Register((t) => ((TaskCompletionSource<bool>) t).SetCanceled(), tcs))
	        {
	            if (task == await Task.WhenAny(task, tcs.Task))
	                return await task;

	            throw new OperationCanceledException(token);
	        }
	    }
	}
}