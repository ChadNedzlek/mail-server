using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Utility;

namespace Vaettir.Mail.Server.FileSystem
{
	public class HashedPasswordUserStore : IUserStore
	{
		private readonly ProtocolSettings _settings;

		public HashedPasswordUserStore(ProtocolSettings settings)
		{
			_settings = settings;
		}

		public async Task<UserData> GetUserWithPasswordAsync(string userName, string password, CancellationToken cancellationToken)
		{
			var user = await GetUserAsync(userName, cancellationToken);
			if (user == null)
				return null;

			var hash = CalculateHash(user.Salt, password, user.Algorithm);

			if (hash.Length != user.Hash.Length)
				return null;
			if (!ConstantTimeEquals(hash, 0, user.Hash, 0, hash.Length))
				return null;

			return new UserData(userName);
		}

		public Task<byte[]> GetSaltForUserAsync(string username, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public bool CanUserSendAs(UserData user, string mailBox)
		{
			return string.Equals(user.MailBox, mailBox);
		}

		public async Task AddUserAsync(string username, string password, CancellationToken token)
		{
			var salt = new byte[64];
			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}

			string passwordAlgorithm = _settings.PasswordAlgorithm;
			var hash = CalculateHash(salt, password, passwordAlgorithm);

			var tempPasswordFile = Path.GetTempFileName();
			using (var tempStream = File.Open(tempPasswordFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			using (var stream = File.Open(_settings.UserPasswordFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			using (var reader = new StreamReader(stream))
			using (var tempWriter = new StreamWriter(tempStream))
			{
				string line = null;
				var replacedUser = false;
				while (await reader.TryReadLineAsync(l => line = l, token))
				{
					var parts = line.Split(new[] {' '}, 3);
					if (parts.Length != 3)
						continue;

					if (parts[0] == username)
					{
						replacedUser = true;
						await tempWriter.WriteLineAsync($"{username} {passwordAlgorithm} {Convert.ToBase64String(salt)} {Convert.ToBase64String(hash)}");
					}
					else
					{
						await tempWriter.WriteLineAsync(line);
					}
				}

				if (!replacedUser)
					await tempWriter.WriteLineAsync($"{username} {passwordAlgorithm} {Convert.ToBase64String(salt)} {Convert.ToBase64String(hash)}");

				await tempWriter.FlushAsync();

				tempStream.Seek(0, SeekOrigin.Begin);
				stream.Seek(0, SeekOrigin.Begin);
				stream.SetLength(0);
				await tempStream.CopyToAsync(stream);
			}
		}

		private static bool ConstantTimeEquals(byte[] x, int xOffset, byte[] y, int yOffset, int length)
		{
			var differentbits = 0;
			for (var i = 0; i < length; i++)
				differentbits |= x[xOffset + i] ^ y[yOffset + i];
			return (1 & (unchecked((uint) differentbits - 1) >> 8)) != 0;
		}

		private byte[] CalculateHash(byte[] salt, string password, string algorithm)
		{
			var algParts = algorithm.ToLowerInvariant().Split(':');
			switch (algParts[0])
			{
				case "db":
					switch (algParts[1])
					{
						case "sha1":
							int iterationCount = Int32.Parse(algParts[2]);
							using (var alg = new Rfc2898DeriveBytes(password, salt, iterationCount))
							{
								return alg.GetBytes(32);
							}
						default:
							throw new ArgumentException("Unsupported algorithm", nameof(algorithm));
					}
				default:
					throw new ArgumentException("Unsupported algorithm", nameof(algorithm));
			}
		}


		private async Task<FileUserData> GetUserAsync(string username, CancellationToken token)
		{
			using (var stream = File.Open(_settings.UserPasswordFile, FileMode.Open, FileAccess.Read, FileShare.Read))
			using (var reader = new StreamReader(stream))
			{
				string line = null;
				while (await reader.TryReadLineAsync(l => line = l, token))
				{
					var parts = line.Split(' ');
					if (parts.Length != 4)
						continue;

					if (parts[0] == username)
						return new FileUserData
						{
							Name = username,
							Algorithm = parts[1],
							Salt = Convert.FromBase64String(parts[2]),
							Hash = Convert.FromBase64String(parts[3])
						};
				}
			}

			return null;
		}

		private class FileUserData
		{
			public string Name { get; set; }
			public string Algorithm { get; set; }
			public byte[] Salt { get; set; }
			public byte[] Hash { get; set; }
		}
	}
}