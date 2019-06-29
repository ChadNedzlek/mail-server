using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Vaettir.Utility
{
	public sealed class PrivateKeyProvider
	{
		private readonly ImmutableDictionary<string, PrivateKeyHolder> _keys;

		public PrivateKeyProvider(ImmutableDictionary<string, PrivateKeyHolder> keys)
		{
			_keys = keys;
		}

		public PrivateKeyHolder GetKey(string key)
		{
			return _keys.GetValueOrDefault(key);
		}
	}
}