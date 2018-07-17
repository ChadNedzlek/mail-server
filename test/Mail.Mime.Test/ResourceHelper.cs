using System;
using System.IO;

namespace Vaettir.Mime.Test
{
	public static class ResourceHelper
	{
		public static Stream GetResource(this Type type, string name)
		{
			return type.Assembly.GetManifestResourceStream(type.Namespace + "." + name);
		}
	}
}