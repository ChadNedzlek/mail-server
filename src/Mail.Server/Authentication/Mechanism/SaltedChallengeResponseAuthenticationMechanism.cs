using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.Authentication.Mechanism
{
	[AuthenticationMechanism]
	public class SaltedChallengeResponseAuthenticationMechanism : IAuthenticationMechanism
	{
		public string Name => "SCRAM-SHA-1";
		public bool RequiresEncryption => false;

		public IAuthenticationSession CreateSession(IAuthenticationTransport session)
		{
			return new Implementation(session);
		}

		private byte[] Hi(HMAC hmac, string password, byte[] salt, int iterationCount)
		{
			if (iterationCount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(iterationCount), "Iteration count must be a positive integer");
			}

			byte[] key = Encoding.UTF8.GetBytes(password);
			byte [] data = new byte[salt.Length + 4];
			data[data.Length - 1] = 1;
			hmac.Key = key;
			
			byte[] hash = hmac.ComputeHash(data);
			byte [] result = ArrayUtil.Clone(hash);
            for (int i = 1; i < iterationCount; i++)
			{
				hash = hmac.ComputeHash(hash);
				for(int j=0; j<result.Length; j++)
				{
					result[j] ^= hash[j];
				}
			}

			return result;
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
				byte[] bytes = await _session.ReadAuthenticationFragmentAsync(token);
				string clientFirstMessage = Encoding.UTF8.GetString(bytes);
				string[] initialParts = clientFirstMessage.Split(new[] { ',' }, 2);

				if (initialParts.Length != 2)
				{
					throw new ArgumentException();
				}

				string username = null;
				string nonce = null;
				string reserved = null;
				if (!Parse(
					initialParts[1],
					new Dictionary<char, Action<string>>
					{
						{'n', v => username = v},
						{'r', v => nonce = v},
						{'m', v => reserved = v},
					}))
				{
					throw new ArgumentException();
				}

				if (String.IsNullOrEmpty(username) ||
					String.IsNullOrEmpty(nonce))
				{
					throw new ArgumentException();
				}

				byte [] salt = await userStore.GetSaltForUserAsync(username, token);

				Random r = new Random();
				int iterationCount = r.Next(1000, 1500);
				StringBuilder serverNonce = new StringBuilder(10);
				for (int i = 0; i < 10; i++)
				{
					serverNonce.Append((char) r.Next(0x2d, 0x7f));
				}

				string serverFirstMessage = String.Format(
					"r={0}{1},s={2},i={3}",
					nonce,
					serverNonce.ToString(),
					Convert.ToBase64String(salt),
					iterationCount);
				await _session.SendAuthenticationFragmentAsync(
					Encoding.UTF8.GetBytes(
						serverFirstMessage),
					token);

				byte[] clientResponse = await _session.ReadAuthenticationFragmentAsync(token);
				string clientResponseString = Encoding.UTF8.GetString(clientResponse);

				string channelBinding = null;
				string responseNonce = null;
				string proofString = null;
				if (!Parse(
					clientResponseString,
					new Dictionary<char, Action<string>>
					{
						{'c', v => channelBinding = v},
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

					Action<string> handler;
					if (!handlers.TryGetValue(code, out handler))
					{
						continue;
					}

					handler(value);
				}
				return true;
			}
		}
	}
}