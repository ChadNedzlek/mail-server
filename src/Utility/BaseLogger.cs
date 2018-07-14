using System;

namespace Vaettir.Utility
{
	public abstract class BaseLogger : ILogger
	{
		private readonly IVolatile<LogSettings> _baseSettings;
		private readonly IVolatile<LogSettings> _specificSettings;

		public BaseLogger(
			IVolatile<LogSettings> specificSettings,
			IVolatile<LogSettings> baseSettings)
		{
			_specificSettings = specificSettings;
			_baseSettings = baseSettings;
		}

		public virtual void Verbose(int eventId, string message)
		{
			Trace(LogLevel.Verbose, eventId, message, null);
		}

		public virtual void Information(int eventId, string message)
		{
			Trace(LogLevel.Information, eventId, message, null);
		}

		public virtual void Warning(int eventId, string message)
		{
			Trace(LogLevel.Warning, eventId, message, null);
		}

		public virtual void Error(int eventId, string message, Exception exception)
		{
			Trace(LogLevel.Error, eventId, message, exception);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual bool ShouldTrace(int eventId, LogLevel level)
		{
			LogLevel? specificFilter = _specificSettings?.Value?.LevelFilter;
			if (specificFilter.HasValue)
			{
				return level >= specificFilter.Value;
			}

			LogLevel? baseFilter = _baseSettings?.Value?.LevelFilter;
			if (baseFilter.HasValue)
			{
				return level >= baseFilter.Value;
			}

			return true;
		}

		public virtual void Trace(LogLevel level, int eventId, string message, Exception exception)
		{
			if (!ShouldTrace(eventId, level))
			{
				return;
			}

			TraceInternal(level, eventId, message, exception);
		}

		protected abstract void TraceInternal(LogLevel level, int eventId, string message, Exception exception);

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
