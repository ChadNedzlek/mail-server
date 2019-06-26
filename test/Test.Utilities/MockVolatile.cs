using Vaettir.Utility;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockVolatile<T> : IVolatile<T>
	{
		public MockVolatile(T value)
		{
			Value = value;
		}

		public T Value { get; private set; }
		public event ValueChanged<T> ValueChanged;

		public void Dispose()
		{
		}

		public void SetValue(T value)
		{
			T oldValue = value;
			Value = value;
			ValueChanged?.Invoke(this, value, oldValue);
		}
	}
}
