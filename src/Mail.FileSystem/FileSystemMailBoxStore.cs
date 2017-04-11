using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server.Smtp;

namespace Vaettir.Mail.Server.FileSystem
{
	public class FileSystemMailBoxStore : IMailBoxStore
	{
		private class MBoxReference
		{
		}
		private readonly SmtpSettings _settings;

		public FileSystemMailBoxStore(SmtpSettings settings)
		{
			_settings = settings;
		}

		public Task<IMailWriteReference> NewMailAsync(string mailbox, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task<IMailReadReference> OpenReadAsync(IMailReference reference, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task SaveAsync(IMailWriteReference reference, CancellationToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task DeleteAsync(IMailReference reference)
		{
			throw new System.NotImplementedException();
		}
	}
}