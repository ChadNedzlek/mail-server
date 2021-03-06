using System;
using JetBrains.Annotations;

namespace Vaettir.Mail.Server.Imap.Commands
{
	[AttributeUsage(AttributeTargets.Class)]
	[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	public class ImapCommandAttribute : Attribute, IImapCommandMetadata
	{
		public ImapCommandAttribute(string name, SessionState minimumState)
		{
			Name = name;
			MinimumState = minimumState;
		}

		public string Name { get; }
		public SessionState MinimumState { get; }
	}
}
