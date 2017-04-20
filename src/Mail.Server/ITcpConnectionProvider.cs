namespace Vaettir.Mail.Server
{
	public interface ITcpConnectionProvider
	{
		ITcpClient GetClient();
	}
}
