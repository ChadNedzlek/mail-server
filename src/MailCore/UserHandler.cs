using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace MailCore
{
	[Injected]
	internal class UserHandler : CommandHandler
	{
		private readonly IUserStore _userStore;

		public UserHandler(IUserStore userStore)
		{
			_userStore = userStore;
		}

		public override async Task<int> RunAsync(List<string> remaining)
		{
			if (remaining.Count < 1)
			{
				ShowHelp(Console.Error, "User command required", null, null);
				return 1;
			}

			string command = remaining[0];
			remaining.RemoveAt(0);

			switch (command)
			{
				case "add":
				{
					string username = null;
					string password = null;
					OptionSet p = new OptionSet()
						.Add("user|username|u=", "User name", u => username = u)
						.Add("password|pwd|p=", "password", v => password = v);

					remaining = p.Parse(remaining);

					if (remaining.Count != 0)
					{
						ShowHelp(Console.Error, $"Unrecognized argument '{remaining[0]}'", p, null);
						return 1;
					}

					await _userStore.AddUserAsync(username, password, CancellationToken.None);
					return 0;
				}

				default:
					ShowHelp(Console.Error, $"Unrecognized user command '{command}'", null, null);
					return 1;
			}
		}

		private static void ShowHelp(TextWriter textWriter, string message, OptionSet p, Exception exception)
		{
			if (message != null)
			{
				textWriter.WriteLine(message);
				textWriter.WriteLine();
			}

			if (exception != null)
			{
				textWriter.WriteLine($"ERROR: {exception.Message}");
				textWriter.WriteLine();
			}

			textWriter.WriteLine(
				@"Usage:
  vmail [global options] mail <command>

    add -u <user> -p <password>

  global options:
");

			p?.WriteOptionDescriptions(textWriter);
		}
	}
}
