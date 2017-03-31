using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;

namespace Mail.Smtp.Test
{
    public class MockPlainTextAuth : IAuthenticationSession
    {
        public Task<UserData> AuthenticateAsync(bool hasInitialResponse, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}