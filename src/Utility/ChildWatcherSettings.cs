using System;
using System.Threading;

namespace Vaettir.Utility
{
	public static class ChildWatcherSettings
	{
		public static ChildWatcherSettings<TParent, TValue> Create<TParent, TValue>(
			IVolatile<TParent> parent,
			Func<TParent, TValue> getChild) where TValue : class
		{
			return new ChildWatcherSettings<TParent, TValue>(parent, getChild);
		}
	}

	public sealed class ChildWatcherSettings<TParent,TValue> : IVolatile<TValue> where TValue : class
	{
		private readonly IVolatile<TParent> _parent;
		private readonly Func<TParent, TValue> _getChild;
		private TValue _value;

		public ChildWatcherSettings(IVolatile<TParent> parent, Func<TParent, TValue> getChild)
		{
			_parent = parent;
			_parent.ValueChanged += ParentValueChanged;
			_getChild = getChild;
			_value = getChild(parent.Value);
		}

		public TValue Value => _value;

		public event ValueChanged<TValue> ValueChanged;

		public void Dispose()
		{
			_parent.ValueChanged -= ParentValueChanged;
		}

		private void ParentValueChanged(object sender, TParent newvalue, TParent oldvalue)
		{
			var newChildValue = _getChild(newvalue);
			var oldChildValue = Value;
			Interlocked.Exchange(ref _value, newChildValue);
			ValueChanged?.Invoke(sender, newChildValue, oldChildValue);
		}
	}
}