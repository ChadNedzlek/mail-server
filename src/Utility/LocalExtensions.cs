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

	public static class DitionaryExtensions
	{
		public static TValue GetValueOrDefault<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			TValue defaultValue = default(TValue))
		{
			return dict.TryGetValue(key, out var value) ? value : defaultValue;
		}

		public static TValue GetOrAdd<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			TValue newValue)
		{
			if (dict.TryGetValue(key, out var value))
				return value;
			dict.Add(key, newValue);
			return newValue;
		}

		public static TValue GetOrAdd<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			Func<TValue> newValueFunc)
		{
			if (dict.TryGetValue(key, out var value))
				return value;

			TValue newValue = newValueFunc();
			dict.Add(key, newValue);
			return newValue;
		}

		public static TValue GetOrAdd<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			Func<TKey, TValue> newValueFunc)
		{
			if (dict.TryGetValue(key, out var value))
				return value;

			TValue newValue = newValueFunc(key);
			dict.Add(key, newValue);
			return newValue;
		}

		public static TValue AddOrUpdate<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			TValue newValue,
			Func<TKey, TValue, TValue> updateFunc)
		{
			if (dict.TryGetValue(key, out var value))
			{
				newValue = updateFunc(key, value);
				dict[key] = newValue;
				return newValue;
			}

			dict.Add(key, newValue);
			return newValue;
		}

		public static TValue AddOrUpdate<TKey, TValue>(
			this IDictionary<TKey, TValue> dict,
			TKey key,
			Func<TKey, TValue> newValueFunc,
			Func<TKey, TValue, TValue> updateFunc)
		{
			if (dict.TryGetValue(key, out var value))
			{
				var newValue = updateFunc(key, value);
				dict[key] = newValue;
				return newValue;
			}

			{
				var newValue = newValueFunc(key);
				dict.Add(key, newValue);
				return newValue;
			}
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
