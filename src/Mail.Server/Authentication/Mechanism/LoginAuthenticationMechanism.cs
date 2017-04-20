using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
	[AuthenticationMechanism("LOGIN", true)]
	public class LoginAuthenticationMechanism : IAuthenticationSession
	{
		private readonly IAuthenticationTransport _session;
		private readonly IUserStore _userStore;

		public LoginAuthenticationMechanism(IAuthenticationTransport session, IUserStore userStore)
		{
			_session = session;
			_userStore = userStore;
		}

		public async Task<UserData> AuthenticateAsync(bool hasInitialResponse, CancellationToken token)
		{
			if (hasInitialResponse)
			{
				throw new ArgumentException();
			}

			await _session.SendAuthenticationFragmentAsync(Encoding.UTF8.GetBytes("Username:"), token);
			string username = Encoding.UTF8.GetString(await _session.ReadAuthenticationFragmentAsync(token));
			await _session.SendAuthenticationFragmentAsync(Encoding.UTF8.GetBytes("Password:"), token);
			string password = Encoding.UTF8.GetString(await _session.ReadAuthenticationFragmentAsync(token));

			UserData userData = await _userStore.GetUserWithPasswordAsync(username, password, token);

			return userData;
		}
	}
}
