using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vaettir.Mail.Server
{
	public sealed class SecurableConnection : IConnectionSecurity, IVariableStreamReader
	{
		private RedirectableStream _current;
		private SslStream _encrypted;
		private Stream _source;
		private TcpClient _tcp;
		private VariableStreamReader _variableReader;

		public SecurableConnection([NotNull] Stream source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			Init(source);
		}

		public SecurableConnection(TcpClient tcp)
		{
			_tcp = tcp ?? throw new ArgumentNullException(nameof(tcp));
			Init(_tcp.GetStream());
		}

		public RemoteCertificateValidationCallback RemoteValidationCallback { get; set; }
		public LocalCertificateSelectionCallback LocalCertSelectionCallback { get; set; }
		public SecurableConnectionState State { get; private set; }

		public X509Certificate2 Certificate { get; set; }
		public bool IsEncrypted => State == SecurableConnectionState.Secured;

		public void Dispose()
		{
			_variableReader?.Dispose();
			_variableReader = null;
			_encrypted?.Dispose();
			_encrypted = null;
			_source?.Dispose();
			_source = null;
			_current = null;
			_tcp?.Dispose();
			_tcp = null;
		}

		public async Task<string> ReadLineAsync(Encoding encoding, CancellationToken cancellationToken)
		{
			if (State == SecurableConnectionState.Closed)
			{
				throw new ObjectDisposedException(nameof(SecurableConnection));
			}

			return await _variableReader.ReadLineAsync(encoding, cancellationToken);
		}

		public Task<int> ReadBytesAsync(byte[] read, int offset, int count, CancellationToken cancellationToken)
		{
			if (State == SecurableConnectionState.Closed)
			{
				throw new ObjectDisposedException(nameof(SecurableConnection));
			}

			return _variableReader.ReadBytesAsync(read, offset, count, cancellationToken);
		}

		public long BytePositition => _variableReader.BytePositition;

		private void Init(Stream source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			_current = new RedirectableStream(_source = source);
			_variableReader = new VariableStreamReader(_current);
			State = SecurableConnectionState.Open;
		}

		public async Task NegotiateTlsAsync()
		{
			if (State != SecurableConnectionState.Open)
			{
				throw new InvalidOperationException();
			}

			_current.ChangeSteam(
				_encrypted =
					new SslStream(
						_source,
						true,
						RemoteValidationCallback,
						LocalCertSelectionCallback,
						EncryptionPolicy.RequireEncryption));

			await _encrypted.AuthenticateAsServerAsync(Certificate, false, SslProtocols.Tls12, false);
			State = SecurableConnectionState.Secured;
		}

		public async Task NegotiateTlsClientAsync(string targetHost)
		{
			if (State != SecurableConnectionState.Open)
			{
				throw new InvalidOperationException();
			}

			_current.ChangeSteam(
				_encrypted =
					new SslStream(
						_source,
						true,
						RemoteValidationCallback,
						LocalCertSelectionCallback,
						EncryptionPolicy.RequireEncryption));

			await _encrypted.AuthenticateAsClientAsync(targetHost, null, SslProtocols.Tls12, false);
			State = SecurableConnectionState.Secured;
		}

		public void Close()
		{
			Dispose();
		}

		public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (State == SecurableConnectionState.Closed)
			{
				throw new ObjectDisposedException(nameof(SecurableConnection));
			}

			return _current.WriteAsync(buffer, offset, count, cancellationToken);
		}

		public Task WriteAsync(string text, Encoding encoding, CancellationToken cancellationToken)
		{
			if (State == SecurableConnectionState.Closed)
			{
				throw new ObjectDisposedException(nameof(SecurableConnection));
			}

			byte[] buffer = encoding.GetBytes(text);
			return _current.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
		}

		public Task WriteLineAsync(string text, Encoding encoding, CancellationToken cancellationToken)
		{
			if (State == SecurableConnectionState.Closed)
			{
				throw new ObjectDisposedException(nameof(SecurableConnection));
			}

			byte[] buffer = encoding.GetBytes(text + "\r\n");
			return _current.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
		}
	}
}
