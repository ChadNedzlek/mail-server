using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vaettir.Mail.Server;
using Vaettir.Utility;

namespace MailCore
{
	[Injected]
	internal class AgentHandler : CommandHandler
	{
		private readonly MailDispatcher _dispatcher;
		private readonly ProtocolListener _protocol;
		private readonly MailTransfer _transfer;
		private readonly AgentSettings _settings;
		private readonly ILogger _logger;
		private readonly CancellationToken _cancellationToken;

		public AgentHandler(
			ProtocolListener protocol,
			MailDispatcher dispatcher,
			MailTransfer transfer,
			AgentSettings settings,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			_protocol = protocol;
			_dispatcher = dispatcher;
			_transfer = transfer;
			_settings = settings;
			_logger = logger;
			_cancellationToken = cancellationToken;
		}

		public override async Task<int> RunAsync(List<string> remaining)
		{
			if (!string.IsNullOrEmpty(_settings.ServiceAccountName))
			{
				if (!ChangeUserAccount(_settings.ServiceAccountName))
				{
					return 2;
				}
			}

			await Task.WhenAll(
				Task.Run(() => _protocol.RunAsync(_cancellationToken), _cancellationToken),
				Task.Run(() => _dispatcher.RunAsync(_cancellationToken), _cancellationToken),
				Task.Run(() => _transfer.RunAsync(_cancellationToken), _cancellationToken)
			);

			return 0;
		}

		private bool ChangeUserAccount(string accountName)
		{
			_logger.Information($"Changing to service account {accountName}");
			IntPtr userPtr = LinuxLibC.GetPasswordStruct(accountName);
			if (userPtr == IntPtr.Zero)
			{
				_logger.Error($"Could not find user '{accountName}'");
				return false;
			}

			LinuxPasswordStruct user = Marshal.PtrToStructure<LinuxPasswordStruct>(userPtr);

			if (LinuxLibC.SetGid(user.Gid) is int gidResult && gidResult != 0)
			{
				_logger.Error($"Not change to gid {user.Gid}, result code {gidResult}'");
				return false;
			}

			if (LinuxLibC.SetUid(user.Uid) is int uidResult && uidResult != 0)
			{
				_logger.Error($"Not change to uid {user.Uid}, result code {uidResult}'");
				return false;
			}

			return true;
		}
	}

	internal static class LinuxLibC
	{
		[DllImport("libc", EntryPoint = "getpwnam", CharSet = CharSet.Ansi)]
		internal static extern IntPtr GetPasswordStruct(string name);
		
		[DllImport("libc", EntryPoint = "setuid")]
		internal static extern int SetUid(int uid);

		[DllImport("libc", EntryPoint = "getuid")]
		internal static extern int GetUid();

		[DllImport("libc", EntryPoint = "setgid")]
		internal static extern int SetGid(int uid);

		[DllImport("libc", EntryPoint = "getgid")]
		internal static extern int GetGid();
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	internal struct LinuxPasswordStruct
	{
		public string Name;
		public string Password;
		public int Uid;
		public int Gid;
		public string UserInformation;
		public string Home;
		public string Shell;
	}
}
