using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MailServer;
using Vaettir.Utility;

namespace Vaettir.Mail.Server
{
	public abstract class ProtocolListener<TSession> where TSession : IProtocolSession
	{
		private List<TSession> _sessions = new List<TSession>();
		private readonly SemaphoreSlim _sessionSemaphore = new SemaphoreSlim(1);
		private List<Task> _sessionLifetimes = new List<Task>();
		private readonly ImmutableList<int> _ports;

		protected ProtocolListener(string ports)
		{
			_ports = ImmutableList.CreateRange(
				ports
					.Split(';', ',', ' ')
					.Select(Int32.Parse));
		}

		protected ProtocolListener(IEnumerable<int> ports)
		{
			_ports = ImmutableList.CreateRange(ports);
		}

		protected abstract TSession InitiateSession(SecurableConnection connection, EndPoint local, EndPoint remote);

		public async Task Start(CancellationToken cancellationToken)
		{
			TcpListener[] listeners = _ports
				.Select(p => new TcpListener(IPAddress.Any, p))
				.ToArray();

			foreach (var l in listeners)
			{
				l.Start();
			}

			Task<TcpClient>[] tasks =
				listeners.Select(tcp => tcp.AcceptTcpClientAsync())
					.ToArray();

			while (!cancellationToken.IsCancellationRequested)
			{
				int taskCompleted = Task.WaitAny(tasks);

				TcpClient client = tasks[taskCompleted].Result;

				using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
				{
					var securableConnection = new SecurableConnection(client.GetStream());
					securableConnection.Certificate = ServerCertificate;
					var session = InitiateSession(
						securableConnection,
						client.Client.LocalEndPoint,
						client.Client.RemoteEndPoint);
					_sessions.Add(session);
					_sessionLifetimes.Add(session.Start(cancellationToken));
				}

				// Wait for another connection
				tasks[taskCompleted] = listeners[taskCompleted].AcceptTcpClientAsync();
			}

			foreach (var l in listeners)
			{
				l.Stop();
			}
		}

		public X509Certificate2 ServerCertificate { get; set; }

		public async Task Close(CancellationToken cancellationToken)
		{
			using (await SemaphoreLock.GetLockAsync(_sessionSemaphore, cancellationToken))
			{
				Task[] closing = _sessions.Select(session => session.CloseAsync(cancellationToken)).ToArray();
				Task.WaitAll(closing, cancellationToken);
				Task.WaitAll(_sessionLifetimes.ToArray(), cancellationToken);
				_sessions = null;
				_sessionLifetimes = null;
			}
		}
	}
}