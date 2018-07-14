using System;

namespace Vaettir.Mail.Server.Imap
{
	internal class BadImapCommandFormatException : Exception
	{
		public BadImapCommandFormatException(string tag) : base("Bad command")
		{
			Tag = tag ?? "*";
			ErrorMessage = "Bad command";
		}

		public BadImapCommandFormatException(string tag, string message) : base("Bad command: " + message)
		{
			Tag = tag;
			ErrorMessage = message;
		}

		public string Tag { get; }
		public string ErrorMessage { get; }
	}
}
