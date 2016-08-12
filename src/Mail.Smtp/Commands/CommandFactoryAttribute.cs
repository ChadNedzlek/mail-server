using System;
using System.Composition;

namespace Vaettir.Mail.Server.Smtp.Commands
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	internal class CommandFactoryAttribute : ExportAttribute
	{
		public CommandFactoryAttribute() : base(typeof (ICommandFactory))
		{
		}
	}
}