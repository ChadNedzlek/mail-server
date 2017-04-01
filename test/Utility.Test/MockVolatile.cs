using Vaettir.Utility;

namespace Utility.Test
{
	public class MockVolatile<T> : IVolatile<T>
	{
		public MockVolatile(T value)
		{
			Value = value;
		}

		public T Value { get; private set; }
		public event ValueChanged<T> ValueChanged;

		public void SetValue(T value)
		{
			var oldValue = value;
			Value = value;
			ValueChanged?.Invoke(this, value, oldValue);
		}

		public void Dispose()
		{
		}
	}
}