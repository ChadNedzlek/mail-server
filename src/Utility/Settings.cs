using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Vaettir.Utility
{
	public static class Settings
	{
		private static readonly Lazy<IConfiguration> _config = new Lazy<IConfiguration>(BuildConfig);

		private static IConfiguration BuildConfig()
		{
			ConfigurationBuilder builder = new ConfigurationBuilder();
			string rootRelativeConfig = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "smtp.config.json");
			builder.AddJsonFile("smtp.config.json");
			string text = File.ReadAllText(rootRelativeConfig);
			return builder.Build();
		}

		private static readonly ConcurrentDictionary<Type, object> _builtConfigs = new ConcurrentDictionary<Type, object>();

		public static T Get<T>() where T: new()
		{
			Type configType = typeof(T);
			return (T) _builtConfigs.GetOrAdd(configType, t => BindValue(t, _config.Value));
		}

		private static object BindValue(Type type, IConfiguration configuration)
		{
			object value = Activator.CreateInstance(type);
			configuration.GetSection(type.Name).Bind(value);
			return value;
		}
	}
}