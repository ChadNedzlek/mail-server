using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

            byte[] hash = CalculateHash(user.Salt, password);

            if (hash.Length != user.Hash.Length)
                return null;
            if (!ConstantTimeEquals(hash, 0, user.Hash, 0, hash.Length))
                return null;

            return new UserData(userName + "@" + _settings.DomainName);
        }
        public Task<byte[]> GetSaltForUserAsync(string username, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public bool CanUserSendAs(UserData user, string mailBox)
        {
            return String.Equals(user.MailBox, mailBox);
		}

		private static bool ConstantTimeEquals(byte[] x, int xOffset, byte[] y, int yOffset, int length)
		{
			int differentbits = 0;
			for (int i = 0; i < length; i++)
				differentbits |= x[xOffset + i] ^ y[yOffset + i];
			return (1 & (unchecked((uint)differentbits - 1) >> 8)) != 0;
		}

		private byte[] CalculateHash(byte[] salt, string password)
		{
			using (var inc = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
			{
				inc.AppendData(salt);
				inc.AppendData(Encoding.UTF8.GetBytes(password));
				return inc.GetHashAndReset();
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
                    var parts = line.Split(new[] {' '}, 3);
                    if (parts.Length != 3)
                        continue;

                    if (parts[0] == username)
                    {
                        return new FileUserData
                        {
                            Name = username,
                            Salt = Convert.FromBase64String(parts[1]),
                            Hash = Convert.FromBase64String(parts[2])
                        };
                    }
                }
            }

            return null;
        }

        public async Task AddUserAsync(string username, string password, CancellationToken token)
        {
            byte[] salt = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = CalculateHash(salt, password);

            string tempPasswordFile = Path.GetTempFileName();
			using (var tempStream = File.Open(tempPasswordFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			using (var stream = File.Open(_settings.UserPasswordFile, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			using (var reader = new StreamReader(stream))
			using (var tempWriter = new StreamWriter(tempStream))
			{
				string line = null;
			    bool replacedUser = false;
				while (await reader.TryReadLineAsync(l => line = l, token))
				{
					var parts = line.Split(new[] { ' ' }, 3);
					if (parts.Length != 3)
						continue;

				    if (parts[0] == username)
				    {
				        replacedUser = true;
				        await tempWriter.WriteLineAsync($"{username} {Convert.ToBase64String(salt)} {Convert.ToBase64String(hash)}");
				    }
				    else
				    {
				        await tempWriter.WriteLineAsync(line);
				    }
				}

			    if (!replacedUser)
				{
					await tempWriter.WriteLineAsync($"{username} {Convert.ToBase64String(salt)} {Convert.ToBase64String(hash)}");
				}

			    await tempWriter.FlushAsync();

			    tempStream.Seek(0, SeekOrigin.Begin);
			    stream.Seek(0, SeekOrigin.Begin);
			    stream.SetLength(0);
			    await tempStream.CopyToAsync(stream);
			}
        }

        private class FileUserData
        {
            public string Name { get; set; }
            public byte[] Salt { get; set; }
            public byte[] Hash { get; set; }
        }
    }
}