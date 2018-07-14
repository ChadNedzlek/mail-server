using System;
using Autofac.Features.AttributeFilters;

namespace Vaettir.Utility
{
	public enum LogLevel
	{
		Verbose,
		Information,
		Warning,
		Error
	}

	public sealed class ConsoleLogger : BaseLogger
	{
		public ConsoleLogger(
			[KeyFilter("console")] IVolatile<LogSettings> specificSettings,
			IVolatile<LogSettings> baseSettings) : base(specificSettings, baseSettings)
		{
		}

		private static string GetStringFromLevel(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Verbose: return "VERBOSE";
				case LogLevel.Information: return "INFO   ";
				case LogLevel.Warning: return "WARNING";
				case LogLevel.Error: return "ERROR  ";
				default: return "UNKNOWN";
			}
		}

		protected override void TraceInternal(LogLevel level, int eventId, string message, Exception exception)
		{
			Console.WriteLine($"{DateTime.Now:s} {eventId:0000} {GetStringFromLevel(level)} {message}");
		}
	}
}
