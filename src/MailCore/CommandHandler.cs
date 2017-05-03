using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;

namespace MailCore
{
	internal abstract class CommandHandler
	{
		public abstract Task<int> RunAsync(List<string> remaining);
	}
}