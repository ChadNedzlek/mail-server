using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Vaettir.Utility
{
	public class PrivateKeyHolder
	{
		private readonly string _path;
		private readonly DateTimeOffset _modified;
		private readonly X509Certificate2 _certificate;
		private readonly Action _outdatedCallback;

		private PrivateKeyHolder(string path, DateTimeOffset modified, X509Certificate2 certificate, Action outdatedCallback)
		{
			_certificate = certificate;
			_outdatedCallback = outdatedCallback;
			_path = path;
			_modified = modified;
		}

		public bool IsOutdated()
		{
			if (string.IsNullOrEmpty(_path))
				return false;

			return File.GetLastWriteTimeUtc(_path) != _modified;
		}

		public X509Certificate2 GetKey()
		{
			if (IsOutdated())
				_outdatedCallback();
			return _certificate;
		}

		public static async Task<PrivateKeyHolder> LoadAsync(string path, Action outdatedCallback)
		{
			DateTimeOffset modified = File.GetLastWriteTimeUtc(path);
			byte[] bytes = await File.ReadAllBytesAsync(path);
			var cert = new X509Certificate2(bytes);
			return new PrivateKeyHolder(path, modified, cert, outdatedCallback);
		}

		public static PrivateKeyHolder Fixed(X509Certificate2 cert)
		{
			return new PrivateKeyHolder(null, default, cert, null);
		}
	}
}