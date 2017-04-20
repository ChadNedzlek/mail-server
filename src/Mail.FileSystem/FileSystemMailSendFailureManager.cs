using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vaettir.Mail.Server.Smtp;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	public sealed class FileSystemMailSendFailureManager : IMailSendFailureManager
	{
		private readonly Lazy<Dictionary<string, SmtpFailureData>> _failures;
		private readonly ILogger _log;
		private readonly SmtpSettings _settings;

		public FileSystemMailSendFailureManager(SmtpSettings settings, ILogger log)
		{
			_settings = settings;
			_log = log;
			_failures = new Lazy<Dictionary<string, SmtpFailureData>>(LoadSavedFailureData);
		}

		public void SaveFailureData()
		{
			string serializedPath = Path.Combine(_settings.WorkingDirectory, "relay-failures.json");

			if (!_failures.IsValueCreated || _failures.Value.Count == 0)
			{
				try
				{
					File.Delete(serializedPath);
				}
				catch (Exception)
				{
					_log.Warning($"Failed to delete {serializedPath}");
				}

				return;
			}

			using (FileStream stream = File.Open(serializedPath, FileMode.Create, FileAccess.Write, FileShare.None))
			using (var reader = new StreamWriter(stream))
			using (var jsonReader = new JsonTextWriter(reader))
			{
				new JsonSerializer().Serialize(jsonReader, _failures.Value);
			}
		}

		public void RemoveFailure(string mailId)
		{
			_failures.Value.Remove(mailId);
		}

		public SmtpFailureData GetFailure(string mailId, bool createIfMissing)
		{
			if (!createIfMissing && !_failures.IsValueCreated)
			{
				return null;
			}

			SmtpFailureData failure;
			if (!_failures.Value.TryGetValue(mailId, out failure))
			{
				if (!createIfMissing)
				{
					return null;
				}

				failure = new SmtpFailureData(mailId) {FirstFailure = DateTimeOffset.UtcNow, Retries = 0};
				_failures.Value.Add(mailId, failure);
			}

			return failure;
		}

		private Dictionary<string, SmtpFailureData> LoadSavedFailureData()
		{
			string serializedPath = Path.Combine(_settings.WorkingDirectory, "relay-failures.json");
			if (!File.Exists(serializedPath))
			{
				return new Dictionary<string, SmtpFailureData>();
			}

			try
			{
				using (FileStream stream = File.Open(serializedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var reader = new StreamReader(stream))
				using (var jsonReader = new JsonTextReader(reader))
				{
					return new JsonSerializer().Deserialize<Dictionary<string, SmtpFailureData>>(jsonReader);
				}
			}
			catch (IOException e)
			{
				_log.Warning($"Failed to load {serializedPath}, using no existing failures: {e}");
				return new Dictionary<string, SmtpFailureData>();
			}
		}
	}
}
