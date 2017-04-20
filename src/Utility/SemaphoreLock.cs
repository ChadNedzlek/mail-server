using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public sealed class SemaphoreLock : IDisposable
	{
		private SemaphoreSlim _sem;

		private SemaphoreLock(SemaphoreSlim sem)
		{
			_sem = sem;
		}

		public void Dispose()
		{
			Interlocked.Exchange(ref _sem, null)?.Release();
		}

		public static async Task<SemaphoreLock> GetLockAsync(SemaphoreSlim semaphore)
		{
			await semaphore.WaitAsync();
			return new SemaphoreLock(semaphore);
		}

		public static async Task<SemaphoreLock> GetLockAsync(SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			await semaphore.WaitAsync(cancellationToken);
			return new SemaphoreLock(semaphore);
		}
	}
}
