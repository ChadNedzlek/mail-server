using System;
using JetBrains.Annotations;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	[AttributeUsage(AttributeTargets.Class)]
	public class SmtpCommandAttribute : Attribute
	{
		public SmtpCommandAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}
