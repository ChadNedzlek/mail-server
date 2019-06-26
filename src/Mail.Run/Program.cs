using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mono.Options;

namespace Mail.Run
{
	internal class Program
	{
		private static async Task<int> Main(string[] args)
		{
			string userName = null;
			var app = Path.GetFullPath("mail.app.exe");
			var fileNames = new List<string>();
			
			var set = new OptionSet
			{
				{"u|user=", "User to de-escalate to run mail daemon", u => userName = u},
				{"m|mail=", "Mail application process (default mail.app.exe)", a => app = a},
				{"f|file=", "File to pipe to child process", fileNames.Add},
			};

			var remainder = set.Parse(args);

			if (string.IsNullOrEmpty(userName))
			{
				Console.WriteLine($"Missing user, assuming current user '{Environment.UserName}'");
			}

			var mapping = fileNames.ToDictionary(_ => Guid.NewGuid().ToString("N"), x => x);

			var pipeArgs = string.Join(" ", mapping.Select(p => $"--pipe \"{p.Value}={p.Key}\""));
			var passThruArgs = string.Join(" ", remainder.Select(x => $"\"{x}\""));
			var mailAppStart = new ProcessStartInfo(app, $"{pipeArgs} {passThruArgs}");

			if (userName != null)
			{
				mailAppStart.UserName = userName;
			}

			var appExit = new TaskCompletionSource<int>();
			var appExitTask = appExit.Task;
			var monitorCancel = new CancellationTokenSource();
			var mailAppProcess = new Process {StartInfo = mailAppStart, EnableRaisingEvents = true};
			mailAppProcess.Exited += (p, _) =>
			{
				monitorCancel.Cancel();
				try
				{
					appExit.SetResult(((Process) p).ExitCode);
				}
				catch
				{
					appExit.SetResult(-1);
				}
			};

			List<Task> monitors = mapping.Select(p => MonitorFileAsync(p.Key, userName, p.Value, monitorCancel.Token))
				.ToList();

			// If we are getting killed, try and kill the spawned process too
			AppDomain.CurrentDomain.ProcessExit += (_, __) => mailAppProcess.Kill();

			mailAppProcess.Start();
			List<Task> allTasks = new List<Task>(monitors) {appExitTask};

			try
			{
				await Task.WhenAll(allTasks);
			}
			catch (AggregateException e)
			{
				foreach (var ex in e.InnerExceptions)
				{
					if (ex is TaskCanceledException tce && tce.CancellationToken == monitorCancel.Token)
					{
					}
					else
					{
						Console.Error.WriteLine($"Uncaught exception: {ex}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Uncaught exception: {ex}");
			}

			return await appExitTask;
		}

		private static async Task MonitorFileAsync(string name, string user, string file, CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					using (var stream = new NamedPipeServerStream(name,
						PipeDirection.InOut,
						1,
						PipeTransmissionMode.Byte,
						PipeOptions.Asynchronous))
					{
						await stream.WaitForConnectionAsync(token);
						byte[] signal = new byte[1];
						if (await stream.ReadAsync(signal, 0, 1, token) != 1 || signal[0] != 42)
						{
							throw new InvalidOperationException("Missing header value");
						}

						if (!String.IsNullOrEmpty(user) &&
							!String.Equals(user, stream.GetImpersonationUserName(), StringComparison.Ordinal))
						{
							throw new UnauthorizedAccessException(
								$"Connection from disallowed username: {stream.GetImpersonationUserName()}");
						}

						async Task EchoFile(Stream source, Stream target)
						{
							await target.WriteAsync(BitConverter.GetBytes((long) source.Length), token);
							await source.CopyToAsync(target, token);
						}

						string dir = Path.GetDirectoryName(Path.GetFullPath(file));
						string filename = Path.GetFileName(file);

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

							while (!token.IsCancellationRequested)
							{
								var path = await changed.Task;
								Interlocked.Exchange(ref changed, new TaskCompletionSource<string>());
								FileStream fileStream = null;
								while (!changed.Task.IsCompleted && fileStream == null)
								{
									try
									{
										fileStream = File.OpenRead(path);
									}
									catch (IOException)
									{
										await Task.Delay(TimeSpan.FromMilliseconds(500), token);
									}
								}

								using (fileStream)
								{
									await EchoFile(fileStream, stream);
								}
							}
						}
					}
				}
				catch (OperationCanceledException e) when (e.CancellationToken == token)
				{
					throw;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine($"Uncaught exception sending pipe '{name}', restarting: {e}");
				}
			}
		}
	}
}
