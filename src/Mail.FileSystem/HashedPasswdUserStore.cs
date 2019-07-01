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

			byte[] hash = CalculateHash(user.Salt, password, user.Algorithm);

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
			byte[] hash = CalculateHash(salt, password, passwordAlgorithm);

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
							$"{username}:{{{passwordAlgorithm}}}{Convert.ToBase64String(hash.Concat(salt).ToArray())}");
					}
					else
					{
						await tempWriter.WriteLineAsync(line);
					}
				}

				if (!replacedUser)
				{
					await tempWriter.WriteLineAsync(
						$"{username}:{{{passwordAlgorithm}}}{Convert.ToBase64String(hash.Concat(salt).ToArray())}");
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

		private byte[] CalculateHash(byte[] salt, string password, string algorithm)
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
							Convert.FromBase64String(pwdData),
							out fileUserData.Hash,
							out fileUserData.Salt);

						return fileUserData;
					}
				}
			}

			return null;
		}

		private void SplitHashAndSalt(string algorithm, byte[] data, out byte[] hash, out byte[] salt)
		{
			int hashSize;
			switch (algorithm)
			{
				case "SSHA":
					hashSize = 20;
					break;
				case "SSHA256":
					hashSize = 32;
					break;
				case "SSHA512":
					hashSize = 64;
					break;
				default:
					hash = salt = Array.Empty<byte>();
					return;
			}
			hash = new byte[hashSize];
			Array.Copy(data, 0, hash, 0, hashSize);
			salt = new byte[data.Length - hashSize];
			Array.Copy(data, hashSize, salt, 0, data.Length - hashSize);
		}

		private class FileUserData
		{
			public string Name;
			public string Algorithm;
			public byte[] Salt;
			public byte[] Hash;
		}
	}
}
