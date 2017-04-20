using System;
using System.Threading;

namespace Vaettir.Utility
{
	public sealed class Sharable<T> : IDisposable where T : class, IDisposable
	{
		private T _value;

		public Sharable(T value)
		{
			_value = value;
		}

		public bool HasValue => _value != null;

		public void Dispose()
		{
			Interlocked.Exchange(ref _value, null)?.Dispose();
		}

		public T Peek()
		{
			return _value;
		}

		public T TakeValue()
		{
			return Interlocked.Exchange(ref _value, null);
		}
	}

	public static class Sharable
	{
		public static Sharable<T> Create<T>(T value) where T : class, IDisposable
		{
			return new Sharable<T>(value);
		}
	}
}
