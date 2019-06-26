using System.Collections.Generic;
using Autofac.Features.Indexed;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockIndex<TKey, TValue> : Dictionary<TKey, TValue>, IIndex<TKey, TValue>
	{
	}
}
