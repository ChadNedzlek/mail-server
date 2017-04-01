using System;
using Vaettir.Utility;
using Xunit;

namespace Utility.Test
{
	public class StringUtilTests
	{
		[Fact]
		public void Empty()
		{
			var parts = "".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] { "" }, parts);
		}
		[Fact]
		public void Empty_RemoveEmpty()
		{
			var parts = "".SplitQuoted(',', '"', '\\', StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(new string[0], parts);
		}

		[Fact]
		public void SimpleSplit()
		{
			var parts = "a,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] { "a", "b", "c" }, parts);
		}

		[Fact]
		public void WithEmpty_RemoveEmpty()
		{
			var parts = "a,,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(new[] { "a", "b", "c" }, parts);
		}

		[Fact]
		public void WithEmpty_NoRemove()
		{
			var parts = "a,,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] {"a", "", "b", "c"}, parts);
		}

		[Fact]
		public void Quoted()
		{
			var parts = "\"a,a2\",b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] { "\"a,a2\"", "b", "c" }, parts);
		}

		[Fact]
		public void Escaped()
		{
			var parts = "a\\,a2,b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] { "a\\,a2", "b", "c" }, parts);
		}

		[Fact]
		public void EscapedQuoted()
		{
			var parts = "\"a\\\",a2\",b,c".SplitQuoted(',', '"', '\\', StringSplitOptions.None);
			Assert.Equal(new[] { "\"a\\\",a2\"", "b", "c" }, parts);
		}
	}
}