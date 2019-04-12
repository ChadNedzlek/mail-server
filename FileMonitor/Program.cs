using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

namespace FileMonitor
{
	internal class Program
	{
		private static async Task<int> Main(string[] args)
		{
			var client = false;
			string name = null;
			string user = null;
			string file = null;
			var options = new OptionSet
			{
				{"client|connect|c", "Connect to existing pipe as client", c => client = c != null},
				{"server|s", "Create a client as server", c => client = c == null},

				{"name|n=", "Named pipe name", n => name = n},
				{"username|user|u=", "User name to enforce", u => user = u},
				{"file|f=", "File to watch and echo", f => file = f},
			};

			var remaining = options.Parse(args);

			if (remaining.Count > 0 && name == null)
			{
				name = remaining[0];
				remaining.RemoveAt(0);
			}

			if (remaining.Count > 0)
			{
				Console.Error.WriteLine($"Unknown argument: {remaining[0]}");
				return 1;
			}

			if (string.IsNullOrEmpty(name))
			{
				Console.Error.WriteLine("Missing required argument --name");
				return 1;
			}

			if (client)
			{
				using (var stream = new NamedPipeClientStream(".",
					name,
					PipeDirection.InOut,
					PipeOptions.None,
					TokenImpersonationLevel.Impersonation))
				{
					await stream.ConnectAsync();
					stream.WriteByte(42);
					using (var output = Console.OpenStandardOutput())
					{
						while (true)
						{
							byte[] length = new byte[8];
							var read = await stream.ReadAsync(length, 0, length.Length);
							if (read == 0)
							{
								return 0;
							}

							if (read != length.Length)
							{
								Console.Error.WriteLine(
									$"Failed to read length, only got {read} bytes, expected {length.Length}");
								return 2;
							}

							byte[] buffer = new byte[1024];
							long target = BitConverter.ToInt64(length);
							long total = 0;
							while (total < target)
							{
								read = await stream.ReadAsync(buffer, 0, (int) Math.Min(buffer.Length, target - total));
								if (read == 0)
								{
									Console.Error.WriteLine($"Expected {target} found {total} bytes...");
									return 2;
								}

								total += read;
								await output.WriteAsync(buffer, 0, read);
							}
						}
					}
				}
			}
			else
			{

				using (var stream = new NamedPipeServerStream(name,
					PipeDirection.InOut,
					1,
					PipeTransmissionMode.Byte,
					PipeOptions.None))
				{
					await stream.WaitForConnectionAsync();
					if (stream.ReadByte() != 42)
					{
						Console.Error.WriteLine($"Missing magic header value");
						return 1;
					}

					if (!String.IsNullOrEmpty(user) &&
						!String.Equals(user, stream.GetImpersonationUserName(), StringComparison.Ordinal))
					{
						Console.Error.WriteLine(
							$"Connection from disallowed username: {stream.GetImpersonationUserName()}");
						return 1;
					}

					async Task EchoFile(Stream source, Stream target)
					{
						await target.WriteAsync(BitConverter.GetBytes((long)source.Length));
						await source.CopyToAsync(target);
					}

					if (file != null)
					{
						var dir = Path.GetDirectoryName(Path.GetFullPath(file));
						var filename = Path.GetFileName(file);

						using (var watcher = new FileSystemWatcher(dir, filename) {EnableRaisingEvents = true,})
						{
							TaskCompletionSource<string> changed = new TaskCompletionSource<string>();
							watcher.Changed += (o, e) =>
							{
								switch (e.ChangeType)
								{
									case WatcherChangeTypes.Changed:
									case WatcherChangeTypes.Created:
										changed.TrySetResult(e.FullPath);
										break;
								}
							};

							using (var fileStream = File.OpenRead(file))
							{
								await EchoFile(fileStream, stream);
							}

							while (true)
							{
								var path = await changed.Task;
								Interlocked.Exchange(ref changed, new TaskCompletionSource<string>());
								FileStream fileStream;
								while (!changed.Task.IsCompleted && fileStream == null)
								{
									try
									{
										fileStream = File.OpenRead(path);
									}
									catch (IOException)
									{
										await Task.Delay(TimeSpan.FromMilliseconds(500));
									}
								}

								using (fileStream)
								{
									await EchoFile(fileStream, stream);
								}
							}
						}
					}
					else
					{
						await EchoFile(Console.OpenStandardInput(), stream);
					}

				}
			}

			return 0;
		}
	}
}
