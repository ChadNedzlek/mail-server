namespace Vaettir.Mail.Transfer
{
	public interface ITcpConnectionProvider
	{
		ITcpClient GetClient();
	}
}