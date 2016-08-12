using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
	[AuthenticationMechanism]
	public class PlainAuthenticationMechanism : IAuthenticationMechanism
	{
		public bool RequiresEncryption => true;
		public string Name => "PLAIN";

		public IAuthenticationSession CreateSession(IAuthenticationTransport session)
		{
			return new Implementation(session);
		}

		private class Implementation : IAuthenticationSession
		{
			private readonly IAuthenticationTransport _session;

			public Implementation(IAuthenticationTransport session)
			{
				_session = session;
			}

			public async Task<UserData> AuthenticateAsync(IUserStore userStore, CancellationToken token, bool hasInitialResponse)
			{
				if (!hasInitialResponse)
				{
					await _session.SendAuthenticationFragmentAsync(Array.Empty<byte>(), token);
				}

				byte [] data = await _session.ReadAuthenticationFragmentAsync(token);

				string authData = Encoding.UTF8.GetString(data);
				if (authData == null)
				{
					throw new ArgumentException();
				}

				string[] parts = authData.Split('\0');
				if (parts.Length != 3)
				{
					throw new ArgumentException();
				}

				string userName = parts[1];
				string password = parts[2];

				UserData userData = await userStore.GetUserWithPasswordAsync(userName, password);

				return userData;
			}
		}
	}
}