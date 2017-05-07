using System;

namespace Vaettir.Mail.Server.Smtp.Commands
{
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
