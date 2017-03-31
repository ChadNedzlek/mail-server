﻿using System;
using Vaettir.Utility;

namespace Mail.Smtp.Test
{
	public class MockLogger : ILogger
    {
        public void Dispose()
        {
        }

        public void Verbose(int eventId, string message)
        {
        }

        public void Information(int eventId, string message)
        {
        }

        public void Warning(int eventId, string message)
        {
        }

        public void Error(int eventId, string message, Exception exception)
        {
        }
    }
}