using System;

namespace Vaettir.Utility
{
	public static class ChildWatcherSettings
	{
		public static ChildWatcherSettings<TParent, TValue> Create<TParent, TValue>(
			IVolatile<TParent> parent,
			Func<TParent, TValue> getChild)
		{
			return new ChildWatcherSettings<TParent, TValue>(parent, getChild);
		}
	}

	public sealed class ChildWatcherSettings<TParent,TValue> : IVolatile<TValue>
	{
		private readonly IVolatile<TParent> _parent;
		private readonly Func<TParent, TValue> _getChild;

		public ChildWatcherSettings(IVolatile<TParent> parent, Func<TParent, TValue> getChild)
		{
			_parent = parent;
			_parent.ValueChanged += ParentValueChanged;
			_getChild = getChild;
			Value = getChild(parent.Value);
		}

		public TValue Value { get; private set; }
		public event ValueChanged<TValue> ValueChanged;

		public void Dispose()
		{
			_parent.ValueChanged -= ParentValueChanged;
		}

		private void ParentValueChanged(object sender, TParent newvalue, TParent oldvalue)
		{
			var newChildValue = _getChild(newvalue);
			var oldChildValue = Value;
			Value = newChildValue;
			ValueChanged?.Invoke(sender, newChildValue, oldChildValue);
		}
	}
}