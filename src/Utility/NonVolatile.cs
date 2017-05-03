namespace Vaettir.Utility
{
	public sealed class NonVolatile<T> : IVolatile<T>
	{
		public NonVolatile(T value)
		{
			Value = value;
		}

		public void Dispose()
		{
		}

		public T Value { get; }
		public event ValueChanged<T> ValueChanged;
	}
}