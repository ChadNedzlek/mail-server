using System;
using System.Collections.Generic;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Transfer.Test
{
	internal class MockMailSendFailureManager : IMailSendFailureManager
	{
		public Dictionary<string, SmtpFailureData> CurrentFailures = new Dictionary<string, SmtpFailureData>();
		public Dictionary<string, SmtpFailureData> SavedFailures = new Dictionary<string, SmtpFailureData>();

		public void SaveFailureData()
		{
			SavedFailures = new Dictionary<string, SmtpFailureData>();
			CurrentFailures = new Dictionary<string, SmtpFailureData>(CurrentFailures);
		}

		public void RemoveFailure(string mailId)
		{
			CurrentFailures.Remove(mailId);
		}

		public SmtpFailureData GetFailure(string mailId, bool createIfMissing)
		{
			SmtpFailureData failure;
			if (!CurrentFailures.TryGetValue(mailId, out failure))
			{
				if (createIfMissing)
				{
					failure = new SmtpFailureData(mailId) {FirstFailure = DateTimeOffset.UtcNow, Retries = 0};
					CurrentFailures.Add(mailId, failure);
				}
				else
				{
					failure = null;
				}
			}

			return failure;
		}

		public void AddFailure(string mailId, DateTimeOffset failTime, int retries)
		{
			CurrentFailures.Add(mailId, new SmtpFailureData(mailId) {FirstFailure = failTime, Retries = retries});
		}
	}
}
