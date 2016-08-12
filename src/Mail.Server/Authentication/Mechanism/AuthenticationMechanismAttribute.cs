using System;
using System.Composition;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
	[AttributeUsage(AttributeTargets.Class)]
	public class AuthenticationMechanismAttribute : ExportAttribute
	{
		public AuthenticationMechanismAttribute() : base(typeof (IAuthenticationMechanism))
		{
		}
	}
}