using SiraUtil.Logging;
using SiraUtil.Zenject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CameraPlusMovementScriptBox.NonView
{
	internal class SongMenuEvent : IAsyncInitializable, IDisposable
	{
		public record SongSelectionEventArgs(
			string? bsr,
			string? hash);
		public event Action<SongSelectionEventArgs>? SongSelectionChanged;

		private SiraLog? logger;
		private StandardLevelDetailViewController standardLevelDetailViewController;
		private SongDetailsCache.SongDetails? songDetails;

		public SongMenuEvent(
			SiraLog? logger,
			StandardLevelDetailViewController standardLevelDetailViewController)
		{
			this.logger = logger;
			this.standardLevelDetailViewController = standardLevelDetailViewController;
			standardLevelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyBeatmapChanged;
			standardLevelDetailViewController.didChangeContentEvent += OnContentChanged;
		}

		public async Task InitializeAsync(CancellationToken token)
		{
			songDetails = await SongDetailsCache.SongDetails.Init(12);
		}

		public void Dispose()
		{
			standardLevelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyBeatmapChanged;
			standardLevelDetailViewController.didChangeContentEvent -= OnContentChanged;
		}

		private void OnContentChanged(StandardLevelDetailViewController? controller, StandardLevelDetailViewController.ContentType type)
		{
			if (controller == null || controller.beatmapLevel == null)
			{
				return;
			}
			var levelId = controller?.beatmapLevel.levelID;
			NewLevelIdSelected(levelId);
		}

		private void OnDifficultyBeatmapChanged(StandardLevelDetailViewController? controller)
		{
			if (controller == null || controller.beatmapLevel == null)
			{
				return;
			}
			var levelId = controller?.beatmapLevel.levelID;
			NewLevelIdSelected(levelId);
		}

		private void NewLevelIdSelected(string? levelId)
		{
			var songHash = levelId == null
				? null
				: SongCore.Collections.GetCustomLevelHash(levelId);
			var bsr = GetBsrFromHash(songHash);
			SongSelectionChanged?.Invoke(new(bsr, songHash));
		}

		private string? GetBsrFromHash(string? songHash)
		{
			if (songDetails == null)
			{
				logger?.Warn("SongDetailsCache is not initialized yet.");
				return null;
			}
			if(songHash == null)
			{
				return null;
			}
			if (songDetails.songs.FindByHash(songHash, out var song))
			{
				return song.key;
			}
			return null;

		}
	}
}
