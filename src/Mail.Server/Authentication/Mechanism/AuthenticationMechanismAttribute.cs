using System;
using JetBrains.Annotations;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
	[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	[AttributeUsage(AttributeTargets.Class)]
	public class AuthenticationMechanismAttribute : Attribute, IAuthencticationMechanismMetadata
	{
		public AuthenticationMechanismAttribute(string name, bool requiresEncryption)
		{
			Name = name;
			RequiresEncryption = requiresEncryption;
		}

		public string Name { get; }
		public bool RequiresEncryption { get; }
	}

	public interface IAuthencticationMechanismMetadata
	{
		string Name { get; }
		bool RequiresEncryption { get; }
	}
}
