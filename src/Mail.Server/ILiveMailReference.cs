using System;
using System.Collections.Immutable;

namespace Vaettir.Mail.Server
{
	public interface ILiveMailReference : IDisposable
	{
		string Id { get; }
		string Sender { get; }
		IImmutableList<string> Recipients { get; }
		IMailStore Store { get; }
	}
}
