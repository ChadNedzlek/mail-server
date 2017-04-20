using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public static class EnumerableHelpers
	{
		public static IEnumerable<T> ToEnumerable<T>(this T value)
		{
			return new[] {value};
		}
	}

	public static class LocalExtensions
	{
		public static IEnumerable<T> Append<T>(this IEnumerable<T> list, T item)
		{
			return list.Concat(new[] {item});
		}

		public static Task<bool> TryReadLineAsync(this TextReader reader, Action<string> getLine, CancellationToken token)
		{
			return reader.ReadLineAsync()
				.ContinueWith(
					l =>
					{
						string line = l.Result;
						getLine(line);
						return line != null;
					},
					token);
		}

		public static bool TryReadLine(this TextReader reader, out string line)
		{
			line = reader.ReadLine();
			return line != null;
		}
	}
}
