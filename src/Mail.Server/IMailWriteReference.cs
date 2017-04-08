using System.IO;

namespace Vaettir.Mail.Server
{
	public interface IMailWriteReference : ILiveMailReference
	{
		Stream BodyStream { get; }
	}
}