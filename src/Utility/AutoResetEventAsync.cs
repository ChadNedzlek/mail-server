using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public sealed class AutoResetEventAsync
	{
		private static readonly Task Completed = Task.FromResult(true);
		private readonly LinkedList<Data> _waits = new LinkedList<Data>();
		private bool _signaled;

		public AutoResetEventAsync(bool signaled)
		{
			_signaled = signaled;
		}

		public Task WaitAsync()
		{
			return WaitAsync(CancellationToken.None);
		}

		public Task WaitAsync(CancellationToken cancellationToken)
		{
			lock (_waits)
			{
				if (_signaled)
				{
					_signaled = false;
					return Completed;
				}
				var tcs = new TaskCompletionSource<bool>();
				Data data;
				if (cancellationToken.CanBeCanceled)
				{
					Action<object> clear = Cancel;
				    StackTrace trace;
				    try
				    {
				        throw new Exception();
				    }
				    catch (Exception e)
				    {
				        trace = new StackTrace(e, true);
				    }

				    data = new Data(tcs, cancellationToken, clear);
				    data.Registration = cancellationToken.Register(clear, _waits.AddLast(data));
					
				}
				else
				{
					data = new Data(tcs, cancellationToken, null);
					_waits.AddLast(data);
				}
				return tcs.Task;
			}
		}

		private void Cancel(object obj)
		{
			var data = (LinkedListNode<Data>) obj;
			data.Value.Source.TrySetCanceled();
			lock (_waits)
			{
				if (data.List == _waits)
				{
					_waits.Remove(data);
				}
			}
		}

		public void Set()
		{
			Data? toRelease = null;
			lock (_waits)
			{
				if (_waits.Count > 0)
				{
					toRelease = _waits.First.Value;
				    if (toRelease.Value.Cancel != null)
				    {
				        toRelease.Value.Registration.Dispose();
				    }
				    _waits.RemoveFirst();
				}
				else if (!_signaled)
				{
					_signaled = true;
				}
			}

			toRelease?.Source.SetResult(true);
		}

		private struct Data
		{
			public readonly Action<object> Cancel;
		    public readonly TaskCompletionSource<bool> Source;
			public readonly CancellationToken Token;

			public Data(TaskCompletionSource<bool> source, CancellationToken token, Action<object> cancel)
			{
				Source = source;
				Token = token;
				Cancel = cancel;
			}

		    public CancellationTokenRegistration Registration { get; set; }
		}
	}
}