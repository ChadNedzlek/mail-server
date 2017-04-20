using System;
using JetBrains.Annotations;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
	[AttributeUsage(AttributeTargets.Class)]
	public class CommandAttribute : Attribute
	{
		public CommandAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}
