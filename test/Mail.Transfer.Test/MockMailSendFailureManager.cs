using Vaettir.Mail.Server;

namespace Mail.Transfer.Test
{
	internal class MockMailSendFailureManager : IMailSendFailureManager
	{
		public void SaveFailureData()
		{
			throw new System.NotImplementedException();
		}

		public void RemoveFailure(string mailId)
		{
			throw new System.NotImplementedException();
		}

		public SmtpFailureData GetFailure(string mailId, bool createIfMissing)
		{
			throw new System.NotImplementedException();
		}
	}
}