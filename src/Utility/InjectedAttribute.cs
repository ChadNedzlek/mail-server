using System;
using JetBrains.Annotations;

namespace Vaettir.Utility
{
	[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class InjectedAttribute : Attribute
	{
	}
}
