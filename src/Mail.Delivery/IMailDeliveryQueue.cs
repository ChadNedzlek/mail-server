using System;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Delivery
{
	public interface IMailDeliveryQueue
	{
		Task<IDisposable> LockDeliveryQueue(string domain);
	}

	public class FileMailDeliveryQueue
	{
		private object _foo;
	}
}