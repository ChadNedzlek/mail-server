using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Vaettir.Utility
{
	public sealed class PipeResolver : IDisposable
	{
		private readonly Dictionary<string, ReadOnlyMemory<byte>> _values = new Dictionary<string, ReadOnlyMemory<byte>>();
		private readonly Dictionary<string, NamedPipeClientStream> _pipes = new Dictionary<string, NamedPipeClientStream>();
		private readonly Dictionary<string, SemaphoreSlim> _readSemaphores = new Dictionary<string, SemaphoreSlim>();
		private readonly SemaphoreSlim _addSemaphore = new SemaphoreSlim(1);

		private readonly ImmutableDictionary<string, string> _mappings;
		private readonly ILogger _logger;

		private readonly AutoResetEventAsync _addedEvent = new AutoResetEventAsync(false);
		private readonly CancellationTokenSource _stopListening = new CancellationTokenSource();
		private Task _monitorTask;

		public PipeResolver(
			ImmutableDictionary<string, string> mappings,
			ILogger logger)
		{
			_mappings = mappings;
			_logger = logger;
		}

		public async Task<ReadOnlyMemory<byte>> GetValueAsync(string name, CancellationToken token)
		{
			if (!_mappings.ContainsKey(name))
			{
				return null;
			}

			if (_values.TryGetValue(name, out var value))
			{
				return value;
			}

			using (var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, _stopListening.Token))
			{
				await InitializePipeAsync(name, linkedToken.Token);
				await ReadValueAsync(name, linkedToken.Token);
			}

			return _values[name];
		}

		private async Task InitializePipeAsync(string name, CancellationToken token)
		{
			if (!_pipes.TryGetValue(name, out var pipe))
			{
				using (await SemaphoreLock.GetLockAsync(_addSemaphore, token))
				{
					if (!_pipes.TryGetValue(name, out pipe))
					{
						pipe = new NamedPipeClientStream(".",
							_mappings[name],
							PipeDirection.In,
							PipeOptions.None,
							TokenImpersonationLevel.Impersonation);
						await pipe.ConnectAsync(token);
						_pipes.Add(name, pipe);

						byte [] signature = new byte[1];
						int signatureSize = await pipe.ReadAsync(signature, 0, signature.Length, token);
						if (signatureSize != 1 || signature[0] != 42)
						{
							throw new InvalidOperationException("Pipe did not start with correct signature");
						}

						_readSemaphores.GetOrAdd(name, k => new SemaphoreSlim(1));
						_addedEvent.Set();

						if (_monitorTask == null)
							_monitorTask = Task.Run(() => MonitorValuesAsync(_stopListening.Token), token);
					}
				}
			}
		}

		private async Task ReadValueAsync(string name, CancellationToken token)
		{
			while (true)
			{
				token.ThrowIfCancellationRequested();

				NamedPipeClientStream pipe = _pipes[name];
				using (await SemaphoreLock.GetLockAsync(_readSemaphores[name], token))
				{
					byte[] length = new byte[8];
					int read = await pipe.ReadAsync(length, 0, length.Length, token);
					if (read == 0)
					{
						_logger.Warning($"Named pipe {name} did not respond, resetting...");
						await ResetPipe(name, token);
						continue;
					}

					if (read != length.Length)
					{
						_logger.Warning($"Named pipe {name} did not responds with 8 bytes of length, resetting...");
						await ResetPipe(name, token);
						continue;
					}

					byte[] newValue = await pipe.ReadExactlyAsync(read, CancellationToken.None);
					if (newValue == null)
					{
						_logger.Warning($"Named pipe {name} did not responds with correct length of data, resetting...");
						await ResetPipe(name, token);
						continue;
					}

					_values[name] = newValue;
					return;
				}
			}
		}

		private async Task MonitorValuesAsync(CancellationToken token)
		{
			_logger.Information("Initializing pipe monitor thread");
			Dictionary<string, Task> things = new Dictionary<string, Task>();
			while (!token.IsCancellationRequested)
			{
				using (await SemaphoreLock.GetLockAsync(_addSemaphore, token))
				{
					foreach (var name in _pipes.Keys)
					{
#pragma warning disable 4014
						// We really don't want to wait here, we are building up
						// a set of things to monitor one off later
						things.GetOrAdd(name, ReadValueAsync(name, token));
#pragma warning restore 4014
					}
				}

				Task<Task> readSomething = Task.WhenAny(things.Values);
				Task newSomething = _addedEvent.WaitAsync(token);
				Task finished = await Task.WhenAny(readSomething, newSomething);

				if (finished != readSomething)
				{
					// Remove the task that finished reading
					// This will cause us to state a new read next loop
					Task whichOne = await readSomething;
					var completedPair = things.First(p => p.Value == whichOne);
					things.Remove(completedPair.Key);
				}
				else
				{
					// Nothing to do, just recollect the dictionary of "things"
					// since a new one was added
				}
			}
		}

		private async Task ResetPipe(string name, CancellationToken token)
		{
			await ClosePipeAsync(name, token);
			await InitializePipeAsync(name, token);
		}

		private async Task ClosePipeAsync(string name, CancellationToken token)
		{
			using (await SemaphoreLock.GetLockAsync(_addSemaphore, token))
			{
				_pipes.Remove(name);
			}
		}

		public void Dispose()
		{
			_stopListening.Cancel();

			foreach (var sem in _readSemaphores.Values)
			{
				sem?.Dispose();
			}

			_addSemaphore?.Dispose();
		}
	}

	public class PipeResolverOptions
	{
		public ImmutableDictionary<string, string> Mappings;

		public PipeResolverOptions(ImmutableDictionary<string, string> mappings)
		{
			Mappings = mappings;
		}
	}
}