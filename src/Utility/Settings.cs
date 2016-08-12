using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Vaettir.Utility
{
	public static class Settings
	{
		private static readonly Lazy<List<JObject>> _objects = new Lazy<List<JObject>>(BuildConfig);

		private static List<JObject> BuildConfig()
		{
			List<string> configPaths = new List<string> {"smtp.config.json"};
			List<JObject> objects = new List<JObject>();
			foreach (var path in configPaths)
			{
				using (var reader = File.OpenText(path))
				using (var json = new JsonTextReader(reader))
				{
					objects.Add(JObject.Load(json));
				}
			}
			return objects;
		}

		private static readonly ConcurrentDictionary<Type, object> _builtConfigs = new ConcurrentDictionary<Type, object>();

		public static T Get<T>()
		{
			Type configType = typeof(T);
			return (T) _builtConfigs.GetOrAdd(configType, t => BindValue(t, _objects.Value));
		}

		private static object BindValue(Type type, List<JObject> configuration)
		{
			string typeName = type.Name;
			var foundConfig = configuration.Select(c => c[typeName]).FirstOrDefault(c => c != null);
			if (foundConfig == null)
				return null;

			using (JsonReader reader = new JTokenReader(foundConfig))
			{
				return JsonSerializer.CreateDefault().Deserialize(reader, type);
			}
		}
	}
}