using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Vaettir.Utility.Test
{
	public class MultiThreadTests
	{
		[Fact]
		public void DoubleStreamWrite()
		{
			byte[] data = {1, 2, 3, 4, 5};
			using (var stream1 = new MemoryStream())
			using (var stream2 = new MemoryStream())
			{
				using (var stream = new MultiStream(stream1, stream2))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.Equal(data, stream1.ToArray());
				Assert.Equal(data, stream2.ToArray());
			}
		}

		[Fact]
		public void NullArrayConstructorThrows()
		{
			Assert.Throws<ArgumentNullException>(
				() => new MultiStream((Stream[]) null));
		}

		[Fact]
		public void NullEnumerableConstructorThrows()
		{
			Assert.Throws<ArgumentNullException>(
				() => new MultiStream((IEnumerable<Stream>) null));
		}

		[Fact]
		public void NullImmutableListConstructorThrows()
		{
			Assert.Throws<ArgumentNullException>(
				() => new MultiStream((ImmutableList<Stream>) null));
		}

		[Fact]
		public void SingleStreamWrite()
		{
			byte[] data = {1, 2, 3, 4, 5};
			using (var stream1 = new MemoryStream())
			{
				using (var stream = new MultiStream(stream1))
				{
					stream.Write(data, 0, data.Length);
				}

				Assert.Equal(data, stream1.ToArray());
			}
		}
	}
}
