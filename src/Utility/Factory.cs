using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Reflection;

namespace Vaettir.Utility
{
	public class Factory<T> where T : class, IFactory
	{
		private static readonly Lazy<Factory<T>> DefaultLazy =
			new Lazy<Factory<T>>(CreateDefault);

		public static Factory<T> Default => DefaultLazy.Value;

		private static Factory<T> CreateDefault()
		{
			var factory = new Factory<T>();
			factory.ImportDefault();
			return factory;
		}

		protected void ImportDefault()
		{
			ContainerConfiguration config = new ContainerConfiguration();
			config = config.WithAssemblies(GetDefaultAssemblies());
			CompositionHost host = config.CreateContainer();
			Import(host);
		}

		private readonly Dictionary<string, T> _factories = new Dictionary<string, T>();

		public void Import(CompositionHost host)
		{
			foreach (T factory in host.GetExports<T>())
			{
				T existing;
				if (!_factories.TryGetValue(factory.Name, out existing))
				{
					_factories.Add(factory.Name, factory);
				}
			}

			OnImport(host);
		}

		protected virtual void OnImport(CompositionHost host)
		{
		}

		protected virtual IEnumerable<Assembly> GetDefaultAssemblies()
		{
			return new[] {typeof (Factory<T>).GetTypeInfo().Assembly};
		}

		public T Get(string name)
		{
			T factory;
			return _factories.TryGetValue(name, out factory) ? factory : null;
		}

		public IEnumerable<string> GetSupported()
		{
			return _factories.Keys;
		}
	}
}