using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Vaettir.Utility
{
	public sealed class FileWatcherSettings<T> : IVolatile<T> where T : class
	{
		private readonly string _settingsFileName;
		private T _settings;
		private FileSystemWatcher _watcher;

		private FileWatcherSettings(string settingsFileName, T initial)
		{
			_settingsFileName = settingsFileName;
			_settings = initial;
			_watcher = new FileSystemWatcher(
				Path.GetDirectoryName(_settingsFileName),
				Path.GetFileName(_settingsFileName))
			{
				EnableRaisingEvents = true
			};

			_watcher.Changed += FileChanged;

		}

		private void FileChanged(object sender, FileSystemEventArgs e)
		{
			var oldValue = _settings;
			var newValue = ReadSettings(_settingsFileName);
			Interlocked.Exchange(ref _settings, newValue);
			ValueChanged?.Invoke(this, newValue, oldValue);
		}

		public void Dispose()
		{
			_watcher?.Dispose();
			_watcher = null;
		}

		public T Value => _settings;

		public static FileWatcherSettings<T> Load(string filePath)
		{
			return new FileWatcherSettings<T>(filePath, ReadSettings(filePath));
		}

		private static T ReadSettings(string filePath)
		{
			using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var reader = new StreamReader(stream))
			using (var jsonReader = new JsonTextReader(reader))
			{
				return new JsonSerializer().Deserialize<T>(jsonReader);
			}
		}

		public event ValueChanged<T> ValueChanged;
	}
}