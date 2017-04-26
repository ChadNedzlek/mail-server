using System;

namespace Vaettir.Utility
{
	public sealed class ConsoleLogger : ILogger
	{
		public void Verbose(int eventId, string message)
		{
			Trace(eventId, "VERBOSE", message);
		}

		public void Information(int eventId, string message)
		{
			Trace(eventId, "INFO   ", message);
		}

		public void Warning(int eventId, string message)
		{
			Trace(eventId, "WARNING", message);
		}

		public void Error(int eventId, string message, Exception exception)
		{
			Trace(eventId, "ERROR  ", message);
		}

		public void Dispose()
		{
			// No op for console
		}

		private static void Trace(int eventId, string level, string message)
		{
			Console.WriteLine($"{DateTime.Now:s} {eventId:0000} {level} {message}");
		}
	}
}
