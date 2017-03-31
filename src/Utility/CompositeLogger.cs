using System;
using System.Collections.Generic;
using System.Linq;

namespace Vaettir.Utility
{
    public sealed class CompositeLogger : ILogger
    {
        private readonly IList<ILogger> _delegated;

        public CompositeLogger(IEnumerable<ILogger> loggers)
        {
            _delegated = loggers.ToList();
        }

        public void Verbose(int eventId, string message)
        {
            foreach (var l in _delegated)
                l.Verbose(eventId, message);
        }

        public void Information(int eventId, string message)
		{
			foreach (var l in _delegated)
				l.Information(eventId, message);
		}

        public void Warning(int eventId, string message)
		{
			foreach (var l in _delegated)
				l.Warning(eventId, message);
		}

        public void Error(int eventId, string message, Exception exception)
		{
			foreach (var l in _delegated)
				l.Error(eventId, message, exception);
		}

        public void Dispose()
        {
            foreach (var l in _delegated)
                l.Dispose();
        }
    }
}