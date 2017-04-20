using System;

namespace Vaettir.Utility
{
	public interface ILogger : IDisposable
	{
		void Verbose(int eventId, string message);
		void Information(int eventId, string message);
		void Warning(int eventId, string message);
		void Error(int eventId, string message, Exception exception);
	}

	public static class LogExtentions
	{
		public static void Verbose(this ILogger logger, string message)
		{
			logger.Verbose(0, message);
		}

		public static void Information(this ILogger logger, string message)
		{
			logger.Information(0, message);
		}

		public static void Warning(this ILogger logger, string message)
		{
			logger.Warning(0, message);
		}

		public static void Error(this ILogger logger, string message, Exception exception)
		{
			logger.Error(0, message, exception);
		}

		public static void Error(this ILogger logger, int eventId, string message)
		{
			logger.Error(eventId, message, null);
		}

		public static void Error(this ILogger logger, string message)
		{
			logger.Error(0, message, null);
		}
	}
}
