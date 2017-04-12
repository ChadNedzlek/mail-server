using Vaettir.Mail.Transfer;

namespace Mail.Transfer.Test
{
	internal class MockTcpConnectionProvider : ITcpConnectionProvider
	{
		public ITcpClient GetClient()
		{
			throw new System.NotImplementedException();
		}
	}
}