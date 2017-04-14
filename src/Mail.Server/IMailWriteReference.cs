using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public interface IWriter
	{
		Task SaveAsync(IWritable item, CancellationToken token);
	}

	public interface IWritable
	{
		Stream BodyStream { get; }
		IWriter Store { get; }
	}

	public interface IMailWriteReference : ILiveMailReference, IWritable
	{
	}
}