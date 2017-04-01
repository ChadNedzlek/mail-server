using System;
using Vaettir.Utility;
using Xunit.Abstractions;

namespace Utility.Test
{
	public class TestOutputLogger : ILogger
	{
		private readonly ITestOutputHelper _output;

		public TestOutputLogger(ITestOutputHelper output)
		{
			_output = output;
		}

		public void Verbose(int eventId, string message)
		{
			Write(eventId, "VERBOSE", message, null);
		}

		public void Information(int eventId, string message)
		{
			Write(eventId, "INFO   ", message, null);
		}

		public void Warning(int eventId, string message)
		{
			Write(eventId, "WARNING", message, null);
		}

		public void Error(int eventId, string message, Exception exception)
		{
			Write(eventId, "ERROR  ", message, null);
		}

		public void Dispose()
		{
		}

		private void Write(int eventId, string level, string message, Exception exception)
		{
			if (exception == null)
			{
				_output.WriteLine($"{eventId:####} {level} {message}");
			}
			else
			{
				_output.WriteLine($"{eventId:####} {level} {message} Exception:{exception}");
			}
		}
	}
}