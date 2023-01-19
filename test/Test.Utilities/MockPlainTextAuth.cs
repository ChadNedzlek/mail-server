using System;
using System.Threading;
using System.Threading.Tasks;
using Vaettir.Mail.Server;
using Vaettir.Mail.Server.Authentication;

namespace Vaettir.Mail.Test.Utilities
{
	public class MockPlainTextAuth : IAuthenticationSession
	{
		public enum Action
		{
			Throw,
			Null,
			Return
		}

		public const string UserMailbox = "test@vaettir.net.test";
		private readonly Action _action;

		public MockPlainTextAuth(Action action)
		{
			_action = action;
		}

		public Task<UserData> AuthenticateAsync(bool hasInitialResponse, CancellationToken token)
		{
			switch (_action)
			{
				case Action.Throw:
					throw new ArgumentException();
				case Action.Null:
					return Task.FromResult((UserData) null);
				case Action.Return:
					return Task.FromResult(new UserData(UserMailbox));
			}

			throw new NotSupportedException();
		}
	}
}
