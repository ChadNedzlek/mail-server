using System;

namespace Vaettir.Utility
{
	public static class ArrayUtil
	{
		public static bool AreEqual<T>(T[] left, T[] right)
		{
			if (left == right)
			{
				return true;
			}

			if (left == null)
			{
				return false;
			}

			if (right == null)
			{
				return false;
			}

			int length = left.Length;

			if (length != right.Length)
			{
				return false;
			}

			for (var i = 0; i < length; i++)
			{
				if (left[i] == null)
				{
					if (right[i] != null)
					{
						return false;
					}
				}
				else if (!Equals(left[i], right[i]))
				{
					return false;
				}
			}

			return true;
		}

		public static bool AreEqual<T>(T[] left, int leftOffset, T[] right, int rightOffset, int count)
		{
			if (left == null && right == null)
			{
				return true;
			}

			if (left == null)
			{
				return false;
			}

			if (right == null)
			{
				return false;
			}

			int leftCount = Math.Min(count, left.Length - leftOffset);
			int rightCount = Math.Min(count, right.Length - rightOffset);

			if (leftCount != rightCount)
			{
				return false;
			}

			for (var i = 0; i < leftCount; i++)
			{
				int iLeft = leftOffset + i;
				int iRight = rightOffset + i;
				if (left[iLeft] == null)
				{
					if (right[iRight] != null)
					{
						return false;
					}
				}
				else if (!Equals(left[iLeft], right[iRight]))
				{
					return false;
				}
			}

			return true;
		}

		public static T[] Clone<T>(T[] value)
		{
			var data = new T[value.Length];
			Array.Copy(value, 0, data, 0, value.Length);
			return data;
		}
	}
}
