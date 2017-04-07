using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedVariable

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
    [AuthenticationMechanism("SCRAM-SHA-1", false)]
    public class SaltedChallengeResponseAuthenticationMechanism : IAuthenticationSession
    {
        private readonly IAuthenticationTransport _session;
        private readonly IUserStore _userStore;

        public SaltedChallengeResponseAuthenticationMechanism(IAuthenticationTransport session, IUserStore userStore)
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
            byte[] bytes = await _session.ReadAuthenticationFragmentAsync(token);
            string clientFirstMessage = Encoding.UTF8.GetString(bytes);
            string[] initialParts = clientFirstMessage.Split(new[] {','}, 2);

            if (initialParts.Length != 2)
            {
                throw new ArgumentException();
            }

            string username = null;
            string nonce = null;
            if (!Parse(
                initialParts[1],
                new Dictionary<char, Action<string>>
                {
                    {'n', v => username = v},
                    {'r', v => nonce = v},
                    {'m', v => {}},
                }))
            {
                throw new ArgumentException();
            }

            if (String.IsNullOrEmpty(username) ||
                String.IsNullOrEmpty(nonce))
            {
                throw new ArgumentException();
            }

            byte[] salt = await _userStore.GetSaltForUserAsync(username, token);

            Random r = new Random();
            int iterationCount = r.Next(1000, 1500);
            StringBuilder serverNonce = new StringBuilder(10);
            for (int i = 0; i < 10; i++)
            {
                serverNonce.Append((char) r.Next(0x2d, 0x7f));
            }

            string serverFirstMessage = $"r={nonce}{serverNonce},s={Convert.ToBase64String(salt)},i={iterationCount}";
            await _session.SendAuthenticationFragmentAsync(Encoding.UTF8.GetBytes(serverFirstMessage), token);

            byte[] clientResponse = await _session.ReadAuthenticationFragmentAsync(token);
            string clientResponseString = Encoding.UTF8.GetString(clientResponse);

            string responseNonce = null;
            string proofString = null;
            if (!Parse(
                clientResponseString,
                new Dictionary<char, Action<string>>
                {
                    {'c', v => {}},
                    {'r', v => responseNonce = v},
                    {'p', v => proofString = v},
                }))
            {
                throw new ArgumentException();
            }

            if (!String.Equals(responseNonce, nonce + serverNonce) ||
                String.IsNullOrEmpty(proofString))
            {
                throw new ArgumentException();
            }

            byte[] proof = Convert.FromBase64String(proofString);
            throw new NotImplementedException();
        }

        private bool Parse(string valueString, Dictionary<char, Action<string>> handlers)
        {
            string[] parts = valueString.Split(',');
            foreach (var str in parts)
            {
                if (str.Length == 0)
                {
                    continue;
                }

                string value;
                char code = str[0];
                if (str.Length == 1)
                {
                    value = null;
                }
                else
                {
                    if (str[1] != '=')
                    {
                        return false;
                    }
                    value = str.Substring(2);
                }
				
                if (!handlers.TryGetValue(code, out var handler))
                {
                    continue;
                }

                handler(value);
            }
            return true;
		}

		private byte[] Hi(HMAC hmac, string password, byte[] salt, int iterationCount)
		{
			if (iterationCount <= 0)
			{
				throw new ArgumentOutOfRangeException(
					nameof(iterationCount),
					"Iteration count must be a positive integer");
			}

			byte[] key = Encoding.UTF8.GetBytes(password);
			byte[] data = new byte[salt.Length + 4];
			data[data.Length - 1] = 1;
			hmac.Key = key;

			byte[] hash = hmac.ComputeHash(data);
			byte[] result = ArrayUtil.Clone(hash);
			for (int i = 1; i < iterationCount; i++)
			{
				hash = hmac.ComputeHash(hash);
				for (int j = 0; j < result.Length; j++)
				{
					result[j] ^= hash[j];
				}
			}

			return result;
		}
	}
}