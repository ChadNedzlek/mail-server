using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Test.Utilities
{
    public class MockEncryptedAuth : IAuthenticationSession
    {
        public Task<UserData> AuthenticateAsync(bool hasInitialResponse, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}