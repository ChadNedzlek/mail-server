using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	[UsedImplicitly]
	public class ProtocolListener
	{
		private readonly ProtocolSettings _settings;
		private readonly ILifetimeScope _scope;
	    private readonly ILogger _log;
	    private List<SessionHolder> _sessions = new List<SessionHolder>();
		private readonly SemaphoreSlim _sessionSemaphore = new SemaphoreSlim(1);

		public ProtocolListener(ProtocolSettings settings, ILifetimeScope scope, ILogger log)
		{
			_settings = settings;
			_scope = scope;
		    _log = log;
		}

		public async Task RunAsync(CancellationToken cancellationToken)
		{
		    _log.Information($"Opening ports: {String.Join(",", _settings.Ports.Select(p => p.ToString()))}");

			TcpListener[] listeners = _settings.Ports
				.Select(p => new TcpListener(IPAddress.Any, p))
				.ToArray();

			foreach (var l in listeners)
			{
				l.Start();
			}

			Task<TcpClient>[] listenerTasks = listeners
				.Select(tcp => tcp.AcceptTcpClientAsync())
				.ToArray();

			while (!cancellationToken.IsCancellationRequested)
			{
				Task[] tasks = new Task[listenerTasks.Length + _sessions.Count];
				Array.Copy(listenerTasks, 0, tasks, 0, listenerTasks.Length);
				_sessions.Select(s => s.Task).ToList().CopyTo(tasks, listenerTasks.Length);

				int completedIndex = Task.WaitAny(tasks);
				Task task = tasks[completedIndex];

				var tcpTask = task as Task<TcpClient>;
				if (tcpTask != null)
				{
					// This is a new connection task
					TcpClient client = tcpTask.Result;
					await AcceptNewClientAsync(client, cancellationToken);

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

			foreach (var l in listeners)
			{
				l.Stop();
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

		private async Task AcceptNewClientAsync(TcpClient client, CancellationToken cancellationToken)
		{
		    var newScope = _scope.BeginLifetimeScope(
		        b =>
		        {
		            b.RegisterInstance(new ConnectionInformation(
						client.Client.LocalEndPoint.ToString(),
		                client.Client.RemoteEndPoint.ToString()));
		            b.RegisterInstance(new SecurableConnection(client.GetStream())
					{
						Certificate = ServerCertificate
					});
		        }
		    );

			var newSession = newScope.Resolve<IProtocolSession>();

			using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
			{
				_sessions.Add(new SessionHolder(newSession, newSession.RunAsync(cancellationToken), newScope));
			}
		}

		public X509Certificate2 ServerCertificate { get; set; }

		public async Task Close(CancellationToken cancellationToken)
		{
			using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
			{
				Task[] closing = _sessions.Select(session => session.Session.CloseAsync(cancellationToken)).ToArray();
			    foreach (var session in _sessions)
			    {
			        session.Scope.Dispose();
			    }
			    Task.WaitAll(closing, cancellationToken);
				Task.WaitAll(_sessions.Select(s => s.Task).ToArray(), cancellationToken);
				_sessions = null;
			}
		}
	}

	public class ProtocolSettings
	{
		public ProtocolSettings(int[] ports, string domainName, string userPasswordFile)
		{
		    Ports = ports;
			DomainName = domainName;
			UserPasswordFile = userPasswordFile;
		}

		public int[] Ports { get; }
		public string DomainName { get; }
		public string UserPasswordFile { get; }
	}
}
