using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Zenject;

namespace CameraPlusMovementScriptBox.Core
{
	internal class MovementScriptLoadingOperator : IInitializable, IDisposable
	{
		private readonly SiraLog? logger;
		private readonly MovementScriptLoader loader;

		private readonly string watchDirectory;
		private readonly TimeSpan monitoringDelay = TimeSpan.FromSeconds(3);
		private readonly CancellationTokenSource cts = new();

		public MovementScriptLoadingOperator(
			SiraLog? logger,
			MovementScriptLoader loader)
		{
			this.logger = logger;
			this.loader = loader;
			watchDirectory = loader.BaseDirectoryPath;
		}

		public void Dispose()
		{
			cts.Cancel();
			cts.Dispose();
		}

		public void Initialize()
		{
			_ = MonitorFileSystemAsync(cts.Token);
		}

		private FileSystemWatcher? watcher;
		private int isEventObserved = 0;

		private async Task MonitorFileSystemAsync(CancellationToken token)
		{
			logger?.Info($"Start monitoring: {watchDirectory}");
			try
			{
				await Task.Delay(monitoringDelay, token).ConfigureAwait(false);
				EnsureDirectoryExists();

				await Task.Delay(monitoringDelay, token).ConfigureAwait(false);
				loader.Load();

				watcher = new FileSystemWatcher(watchDirectory)
				{
					IncludeSubdirectories = true,
					NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
				};

				watcher.Changed += OnFsEvent;
				watcher.Created += OnFsEvent;
				watcher.Deleted += OnFsEvent;
				watcher.Renamed += OnRenamed;
				watcher.EnableRaisingEvents = true;

				bool ready = false;

				while (!token.IsCancellationRequested)
				{
					await Task.Delay(monitoringDelay, token).ConfigureAwait(false);
					if (Interlocked.Exchange(ref isEventObserved, 0) > 0)
					{
						ready = true;
						continue;
					}
					else if (!ready)
					{
						continue;
					}
					ready = false;

					loader.Load();
				}
			}
			finally
			{
				logger?.Info("Stop monitoring.");
				if (watcher != null)
				{
					watcher.EnableRaisingEvents = false;
					watcher.Changed -= OnFsEvent;
					watcher.Created -= OnFsEvent;
					watcher.Deleted -= OnFsEvent;
					watcher.Renamed -= OnRenamed;
					watcher.Dispose();
					watcher = null;
				}
			}
		}

		private void OnFsEvent(object? sender, FileSystemEventArgs e) => SetSignal();

		private void OnRenamed(object? sender, RenamedEventArgs e) => SetSignal();

		private void SetSignal()
		{
			isEventObserved = 1;
		}

		private void EnsureDirectoryExists()
		{
			if (Directory.Exists(watchDirectory))
			{
				return;
			}

			try
			{
				Directory.CreateDirectory(watchDirectory);
				logger?.Info($"Created directory: {watchDirectory}");
			}
			catch (Exception ex)
			{
				logger?.Error($"Failed to create directory '{watchDirectory}': {ex.Message}");
			}
		}
	}
}
