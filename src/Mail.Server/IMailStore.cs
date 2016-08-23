using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server
{
	public class IMailReference
	{
	}

	public interface ILiveMailReference : IDisposable
	{
		string Sender { get; }
		IImmutableList<string> Recipients { get; }
	}

	public interface IMailReadReference : ILiveMailReference
	{
		Stream BodyStream { get; }
	}

	public interface IMailWriteReference : ILiveMailReference
	{
		Stream BodyStream { get; }
		Task SaveAsync(CancellationToken token);
	}

	public abstract class MailWriteReference : IMailWriteReference
	{
		protected MailWriteReference(string sender, IEnumerable<string> recipients)
		{
			Sender = sender;
			Recipients = ImmutableList.CreateRange(recipients);
		}

		public string Sender { get;  }
		public IImmutableList<string> Recipients { get; }

		public abstract Stream BodyStream { get; }
		public abstract Task SaveAsync(CancellationToken token);
		public abstract void Dispose();
	}

	public interface IMailStore
	{
		Task<IMailWriteReference> NewMailAsync(string sender, IEnumerable<string> recipients, CancellationToken token);

		IEnumerable<IMailReference> GetAllMailReferences();
		Task<IMailReadReference> OpenReadAsync(IMailReference reference);
		Task DeleteAsync(IMailReference reference);
	}
}