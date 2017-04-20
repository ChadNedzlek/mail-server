using System;

namespace Vaettir.Utility
{
	public interface IVolatile<out T> : IDisposable
	{
		T Value { get; }
		event ValueChanged<T> ValueChanged;
	}

	public delegate void ValueChanged<in T>(object sender, T newValue, T oldValue);
}
