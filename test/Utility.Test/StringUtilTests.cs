using System;
using System.Collections.Generic;
using Xunit;

namespace Vaettir.Utility.Test
{
	public class StringUtilTests
	{
		[Fact]
		public void Empty()
		{
			IList<string> parts = "".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {""}, parts);
		}

		[Fact]
		public void Empty_RemoveEmpty()
		{
			IList<string> parts = "".SplitQuoted(',', '"', '\\', StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(new string[0], parts);
		}

		[Fact]
		public void Escaped()
		{
			IList<string> parts = "a\\,a2,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {"a\\,a2", "b", "c"}, parts);
		}

		[Fact]
		public void EscapedQuoted()
		{
			IList<string> parts = "\"a\\\",a2\",b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {"\"a\\\",a2\"", "b", "c"}, parts);
		}

		[Fact]
		public void Quoted()
		{
			IList<string> parts = "\"a,a2\",b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {"\"a,a2\"", "b", "c"}, parts);
		}

		[Fact]
		public void SimpleSplit()
		{
			IList<string> parts = "a,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {"a", "b", "c"}, parts);
		}

		[Fact]
		public void WithEmpty_NoRemove()
		{
			IList<string> parts = "a,,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {"a", "", "b", "c"}, parts);
		}

		[Fact]
		public void WithEmpty_RemoveEmpty()
		{
			IList<string> parts = "a,,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(new[] {"a", "b", "c"}, parts);
		}
	}
}
