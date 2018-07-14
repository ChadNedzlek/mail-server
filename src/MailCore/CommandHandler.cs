using System.Collections.Generic;
using System.Threading.Tasks;

namespace MailCore
{
	internal abstract class CommandHandler
	{
		public abstract Task<int> RunAsync(List<string> remaining);
	}
}
