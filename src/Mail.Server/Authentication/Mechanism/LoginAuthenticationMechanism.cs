using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
	[AuthenticationMechanism]
	public class LoginAuthenticationMechanism : IAuthenticationMechanism
	{
		public string Name => "LOGIN";
		public bool RequiresEncryption => true;

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
				if (hasInitialResponse)
				{
					throw new ArgumentException();
				}

				await _session.SendAuthenticationFragmentAsync(Encoding.UTF8.GetBytes("Username:"), token);
				string username = Encoding.UTF8.GetString(await _session.ReadAuthenticationFragmentAsync(token));
				await _session.SendAuthenticationFragmentAsync(Encoding.UTF8.GetBytes("Password:"), token);
				string password = Encoding.UTF8.GetString(await _session.ReadAuthenticationFragmentAsync(token));

				UserData userData = await userStore.GetUserWithPasswordAsync(username, password);

				return userData;
			}
		}
	}
}