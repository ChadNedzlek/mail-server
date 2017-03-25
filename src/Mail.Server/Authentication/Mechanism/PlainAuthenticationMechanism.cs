using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
    [AuthenticationMechanism("PLAIN", true)]
    public class PlainAuthenticationMechanism : IAuthenticationSession
    {
        private readonly IAuthenticationTransport _session;
        private readonly IUserStore _userStore;

        public PlainAuthenticationMechanism(
			IAuthenticationTransport session,
			IUserStore userStore)
        {
            _session = session;
            _userStore = userStore;
        }

        public async Task<UserData> AuthenticateAsync(bool hasInitialResponse, CancellationToken token)
        {
            if (!hasInitialResponse)
            {
                await _session.SendAuthenticationFragmentAsync(Array.Empty<byte>(), token);
            }

            byte[] data = await _session.ReadAuthenticationFragmentAsync(token);

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

            UserData userData = await _userStore.GetUserWithPasswordAsync(userName, password);

            return userData;
        }
    }
}