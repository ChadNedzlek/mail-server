using System.Collections.Generic;
using Xunit;

namespace Vaettir.Mail.Test.Utilities
{
	public static class SequenceAssert
	{
		public static void SameSet<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			ISet<T> expectedSet = new HashSet<T>(expected);
			ISet<T> actualSet = new HashSet<T>(actual);

			Assert.Subset(expectedSet, actualSet);
			Assert.Superset(expectedSet, actualSet);
		}
	}
}