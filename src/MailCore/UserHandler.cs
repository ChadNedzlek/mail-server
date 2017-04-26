using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;

namespace MailCore
{
	internal class UserHandler : CommandHandler
	{
		public override async Task<int> RunAsync(IContainer container, Options options, List<string> remaining)
		{
			if (remaining.Count < 1)
			{
				Program.ShowHelp(Console.Error, "User command required", null, null);
				return 1;
			}

			string command = remaining[0];
			remaining.RemoveAt(0);

			// Do stuff...

			return 0;
		}
	}
}