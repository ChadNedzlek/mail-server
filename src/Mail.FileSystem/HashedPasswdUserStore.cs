using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	public class HashedPasswdUserStore : IUserStore
	{
		private readonly AgentSettings _settings;

		public HashedPasswdUserStore(AgentSettings settings)
		{
			_settings = settings;
		}

		public async Task<UserData> GetUserWithPasswordAsync(
			string userName,
			string password,
			CancellationToken cancellationToken)
		{
			FileUserData user = await GetUserAsync(userName, cancellationToken);
			if (user == null)
			{
				return null;
			}

			byte[] hash = CalculateHash(user.Salt, password, user.Algorithm, user.Iterations);

			if (hash.Length != user.Hash.Length)
			{
				return null;
			}

			if (!ConstantTimeEquals(hash, 0, user.Hash, 0, hash.Length))
			{
				return null;
			}

			return new UserData(userName);
		}

		public bool CanUserSendAs(UserData user, string mailbox)
		{
			return string.Equals(user.Mailbox, mailbox);
		}


		private static string SaltCharacters = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		public async Task AddUserAsync(string username, string password, CancellationToken token)
		{
			if (username.Contains(':'))
				throw new ArgumentException("Username cannot contain a colon", nameof(username));

			var salt = new byte[64];
			using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}

			string passwordAlgorithm = _settings.PasswordAlgorithm;

			switch (passwordAlgorithm)
			{
				case "PBKDF2":
					// dovecot has a weird way of representing the salt for PBKDF2
					for (var index = 0; index < salt.Length; index++)
					{
						salt[index] = unchecked((byte) SaltCharacters[salt[index] % SaltCharacters.Length]);
					}

					break;
			}

			byte[] hash = CalculateHash(salt, password, passwordAlgorithm, 94857);

			string tempPasswordFile = _settings.UserPasswordFile + ".tmp";
			using (FileStream tempStream = File.Open(tempPasswordFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			using (FileStream stream = File.Open(
				_settings.UserPasswordFile,
				FileMode.Create,
				FileAccess.ReadWrite,
				FileShare.None))
			using (var reader = new StreamReader(stream))
			using (var tempWriter = new StreamWriter(tempStream))
			{
				string line = null;
				var replacedUser = false;
				string userLine;
				switch (passwordAlgorithm)
				{
					case "SSHA":
					case "SSHA256":
					case "SSHA512":
						userLine =
							$"{username}:{{{passwordAlgorithm}}}{Convert.ToBase64String(hash.Concat(salt).ToArray())}";
						break;
					case "PBKDF2":
						userLine =
							$"{username}:{{{passwordAlgorithm}}}$1${Encoding.ASCII.GetString(salt)}$94857${hash.ToHex()}";
						break;
					default:
						return;
				}

				while (await reader.TryReadLineAsync(l => line = l, token))
				{
					string[] parts = line.Split(new[] {' '}, 3);
					if (parts.Length != 3)
					{
						continue;
					}

					if (parts[0] == username)
					{
						replacedUser = true;
						await tempWriter.WriteLineAsync(
							userLine);
					}
					else
					{
						await tempWriter.WriteLineAsync(line);
					}
				}

				if (!replacedUser)
				{
					await tempWriter.WriteLineAsync(
						userLine);
				}

				await tempWriter.FlushAsync();

				tempStream.Seek(0, SeekOrigin.Begin);
				stream.Seek(0, SeekOrigin.Begin);
				stream.SetLength(0);
			}

			File.Replace(tempPasswordFile, _settings.UserPasswordFile, _settings.UserPasswordFile + ".bak");
		}

		private static bool ConstantTimeEquals(byte[] x, int xOffset, byte[] y, int yOffset, int length)
		{
			var differentbits = 0;
			for (var i = 0; i < length; i++)
			{
				differentbits |= x[xOffset + i] ^ y[yOffset + i];
			}

			return (1 & (unchecked((uint) differentbits - 1) >> 8)) != 0;
		}

		private byte[] CalculateHash(byte[] salt, string password, string algorithm, int iterations)
		{
			switch (algorithm)
			{
				case "SSHA":
				{
					using (SHA1 sha = SHA1.Create())
					{
						var passwordBytes = Encoding.UTF8.GetBytes(password);
						sha.TransformBlock(passwordBytes, 0, passwordBytes.Length, passwordBytes, 0);
						sha.TransformBlock(salt, 0, salt.Length, salt, 0);
						return sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
					}
				}

				case "SSHA256":
				{
					using (SHA256 sha = SHA256.Create())
					{
						var passwordBytes = Encoding.UTF8.GetBytes(password);
						sha.TransformBlock(passwordBytes, 0, passwordBytes.Length, passwordBytes, 0);
						sha.TransformBlock(salt, 0, salt.Length, salt, 0);
						return sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
					}
				}

				case "SSHA512":
				{
					using (SHA512 sha = SHA512.Create())
					{
						var passwordBytes = Encoding.UTF8.GetBytes(password);
						sha.TransformBlock(passwordBytes, 0, passwordBytes.Length, passwordBytes, 0);
						sha.TransformBlock(salt, 0, salt.Length, salt, 0);
						return sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
					}
				}

				case "PBKDF2":
				{
					var passwordBytes = Encoding.UTF8.GetBytes(password);
					using (var pbkdf2 = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA1))
					{
						return pbkdf2.GetBytes(20);
					}
				}

				default:
					throw new ArgumentException($"Unsupported algorithm {algorithm}", nameof(algorithm));
			}
		}


		private async Task<FileUserData> GetUserAsync(string username, CancellationToken token)
		{
			using (FileStream stream = File.Open(_settings.UserPasswordFile, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var reader = new StreamReader(stream))
			{
				string line = null;
				while (await reader.TryReadLineAsync(l => line = l, token))
				{
					string[] parts = line.Split(':');
					if (parts.Length != 2)
					{
						continue;
					}

					if (!parts[1].StartsWith('{'))
					{
						continue;
					}

					int endAlgIndex = parts[1].IndexOf('}');
					if (endAlgIndex == -1)
					{
						continue;
					}

					if (parts[0] == username)
					{

						string alg = parts[1].Substring(1, endAlgIndex - 1);
						string pwdData = parts[1].Substring(endAlgIndex + 1);

						var fileUserData = new FileUserData
						{
							Name = username,
							Algorithm = alg,
						};

						SplitHashAndSalt(
							fileUserData.Algorithm,
							pwdData,
							out fileUserData.Hash,
							out fileUserData.Salt,
							out fileUserData.Iterations);

						if (fileUserData.Hash == null)
							return null;

						return fileUserData;
					}
				}
			}

			return null;
		}

		private void SplitHashAndSalt(string algorithm, string data, out byte[] hash, out byte[] salt, out int iterations)
		{
			iterations = 0;
			void SplitHaltAppendSalt(int hashSize, out byte[] h, out byte[] s)
			{
				byte[] byteData = Convert.FromBase64String(data);
				h = new byte[hashSize];
				Array.Copy(byteData, 0, h, 0, hashSize);
				s = new byte[data.Length - hashSize];
				Array.Copy(byteData, hashSize, s, 0, data.Length - hashSize);
			}

			switch (algorithm)
			{
				case "SSHA":
					SplitHaltAppendSalt(20, out hash, out salt);
					return;
				case "SSHA256":
					SplitHaltAppendSalt(32, out hash, out salt);
					return;
				case "SSHA512":
					SplitHaltAppendSalt(64, out hash, out salt);
					return;
				case "PBKDF2":
				{
					hash = salt = null;
					var parts = data.Split("$");
					if (parts[0] != "")
						return;
					if (parts[1] != "1")
						return;
					if (!int.TryParse(parts[3], out iterations))
						return;

					salt = Encoding.ASCII.GetBytes(parts[2]);
					hash = parts[4].FromHex();
				}
					return;

				default:
					hash = salt = null;
					return;
			}

		}

		private class FileUserData
		{
			public string Name;
			public string Algorithm;
			public byte[] Salt;
			public byte[] Hash;
			public int Iterations;
		}
	}
}
