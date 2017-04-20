using System.IO;

namespace Vaettir.Mail.Server
{
	public interface IMailReadReference : ILiveMailReference
	{
		Stream BodyStream { get; }
	}
}
