using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlusMovementScriptBox.Core
{
	internal class PlaylistCreator
	{
		private readonly SiraLog? logger;
		private readonly Configuration.PluginConfig config;
		private readonly MovementScriptLoader loader;

		public PlaylistCreator(
			SiraLog? logger,
			Configuration.PluginConfig config,
			MovementScriptLoader loader)
		{
			this.logger = logger;
			this.config = config;
			this.loader = loader;
		}

		public async Task CreateOrUpdate()
		{
			var scripts = loader.GetAllScripts();
			await CreateOrUpdate(scripts);
		}

		public async Task CreateOrUpdate(IEnumerable<MovementScriptDef> scripts)
		{
			var mng = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager;
			if (!string.IsNullOrWhiteSpace(config.PlaylistSubdirectoryName))
			{
				mng = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager(config.PlaylistSubdirectoryName!);
			}

			var coverResourcePath = "CameraPlusMovementScriptBox.Cover256.png";
			var coverText = BeatSaberPlaylistsLib.Utilities.ImageToBase64(coverResourcePath);
			var list = mng.GetOrAdd(
				"CameraScriptBox",
				() => mng.CreatePlaylist("CameraScriptBox", "CameraScriptBox", "CameraScriptBox", coverText)
			);
			list.Clear();

			var songDetails = await SongDetailsCache.SongDetails.Init(12);
			foreach (var script in scripts)
			{
				if (songDetails.songs.FindByMapId(script.Bsr, out var song))
				{
					list.Add(song.hash, song.songName, script.Bsr, song.levelAuthorName);
				}
				else
				{
					logger?.Warn($"Failed to find song for script {script.Title} with BSR {script.Bsr}");
				}
			}
			mng.StorePlaylist(list);
			mng.RequestRefresh("CameraScriptBox");

			logger?.Info($"Created/Updated playlist with {list.Count} songs.");
		}
	}
}
