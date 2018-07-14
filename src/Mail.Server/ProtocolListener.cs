using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Vaettir.Mail.Server.Authentication;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	[Injected]
	public class ProtocolListener
	{
		private readonly ILogger _log;
		private readonly ILifetimeScope _scope;
		private readonly SemaphoreSlim _sessionSemaphore = new SemaphoreSlim(1);
		private readonly AgentSettings _settings;
		private List<SessionHolder> _sessions = new List<SessionHolder>();

		public ProtocolListener(AgentSettings settings, ILifetimeScope scope, ILogger log)
		{
			_settings = settings;
			_scope = scope;
			_log = log;
		}

		public X509Certificate2 ServerCertificate { get; set; }

		public async Task RunAsync(CancellationToken cancellationToken)
		{
			_log.Information($"Opening ports: {string.Join(",", _settings.Connections.Select(p => p.Port.ToString()))}");

			TcpListener[] listeners = _settings.Connections
				.Select(p => new TcpListener(IPAddress.Any, p.Port))
				.ToArray();

			foreach (TcpListener l in listeners)
			{
				l.Start();
			}

			Task<TcpClient>[] listenerTasks = listeners
				.Select(tcp => tcp.AcceptTcpClientAsync())
				.ToArray();

			while (!cancellationToken.IsCancellationRequested)
			{
				var tasks = new Task[listenerTasks.Length + _sessions.Count];
				Array.Copy(listenerTasks, 0, tasks, 0, listenerTasks.Length);
				_sessions.Select(s => s.Task).ToList().CopyTo(tasks, listenerTasks.Length);

				int completedIndex = Task.WaitAny(tasks);
				Task task = tasks[completedIndex];

				if (task is Task<TcpClient> tcpTask)
				{
					// This is a new connection task
					TcpClient client = tcpTask.Result;
					ConnectionSetting connectionSettings = _settings.Connections[completedIndex];
					await AcceptNewClientAsync(client, connectionSettings, cancellationToken);

					// Wait for another connection
					listenerTasks[completedIndex] = listeners[completedIndex].AcceptTcpClientAsync();
				}
				else
				{
					// One of the existing connections is closing
					using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
					{
						int sessionIndex = completedIndex - listenerTasks.Length;
						SessionHolder closingSession = _sessions[sessionIndex];
						_log.Information($"Closing session {closingSession.Session.Id}...");
						_sessions.RemoveAt(sessionIndex);
						closingSession.Scope.Dispose();
					}
				}
			}

			foreach (TcpListener l in listeners)
			{
				l.Stop();
			}
		}

		private async Task AcceptNewClientAsync(
			TcpClient client,
			ConnectionSetting connectionSettings,
			CancellationToken cancellationToken)
		{
			ILifetimeScope newScope = _scope.BeginLifetimeScope(
				builder =>
				{
					builder.RegisterInstance(
						new ConnectionInformation(
							client.Client.LocalEndPoint.ToString(),
							client.Client.RemoteEndPoint.ToString()));

					builder.RegisterInstance(
							new SecurableConnection(client.GetStream())
							{
								Certificate = ServerCertificate
							})
						.As<SecurableConnection>()
						.As<IConnectionSecurity>()
						.As<IVariableStreamReader>();

					builder.RegisterInstance(connectionSettings);

					builder.Register(c => c.ResolveKeyed<IAuthenticationTransport>(connectionSettings.Protocol))
						.As<IAuthenticationTransport>();
				}
			);

			var newSession = newScope.ResolveKeyed<IProtocolSession>(connectionSettings.Protocol);

			using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
			{
				_sessions.Add(new SessionHolder(newSession, newSession.RunAsync(cancellationToken), newScope));
			}
		}

		public async Task Close(CancellationToken cancellationToken)
		{
			using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
			{
				foreach (SessionHolder session in _sessions)
				{
					session.Scope.Dispose();
				}

				await Task.WhenAll(_sessions.Select(s => s.Task));
				_sessions = null;
			}
		}

		private class SessionHolder
		{
			public SessionHolder(IProtocolSession session, Task task, ILifetimeScope scope)
			{
				Session = session;
				Task = task;
				Scope = scope;
			}

			public IProtocolSession Session { get; }
			public Task Task { get; }
			public ILifetimeScope Scope { get; }
		}
	}
}
