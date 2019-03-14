using System.IO;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IIncomingMailScanner
	{
		Task<Stream> ScanAsync(IMailReadReference mailReference, Stream stream);
	}
}