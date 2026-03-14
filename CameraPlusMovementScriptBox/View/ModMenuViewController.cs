using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using CameraPlusMovementScriptBox.Configuration;
using CameraPlusMovementScriptBox.Core;
using CameraPlusMovementScriptBox.Core.ComponentModel;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace CameraPlusMovementScriptBox.View
{
	internal class ModMenuViewController : IInitializable, IDisposable, ITickable
	{
		internal class MenuHost : HostBase
		{
			private readonly SiraLog? logger;
			private readonly PluginConfig config;
			private readonly PlaylistCreator? playlistCreator;

			private bool _Enabled;
			[UIValue("enabled")]
			public bool Enabled
			{
				get => _Enabled;
				set => SetProperty(ref _Enabled, value, () => config.Enabled = value);
			}

			[UIAction("generate-playlist")]
			public void GeneratePlaylist()
			{
				if (playlistCreator == null)
				{
					return;
				}

				GenerateStatus = "Generating playlist...";
				Action func = async () =>
				{
					try
					{
						await playlistCreator.CreateOrUpdate();
						GenerateStatus = "Playlist generated.";
					}
					catch (Exception ex)
					{
						logger?.Error("Failed to generate playlist.");
						logger?.Error(ex);
						GenerateStatus = $"Failed to generate playlist: {ex.Message}";
					}
				};
				func();
			}

			private string _GenerateStatus = string.Empty;
			[UIValue("generate-status")]
			public string GenerateStatus
			{
				get => _GenerateStatus;
				set => SetProperty(ref _GenerateStatus, value);
			}

			private bool _GeneratePlaylistEnabled;
			[UIValue("generate-playlist-enabled")]
			public bool GeneratePlaylistEnabled
			{
				get => _GeneratePlaylistEnabled;
				set => SetProperty(ref _GeneratePlaylistEnabled, value);
			}

			public override void ActionOnModelChanged()
            {
				Enabled = config.Enabled;
			}

			public MenuHost(
				SiraLog? logger,
				PluginConfig config,
				PlaylistCreator? playlistCreator)
			{
				this.logger = logger;
				this.config = config;
				this.playlistCreator = playlistCreator;
				ActionOnModelChanged();
				if (playlistCreator != null)
				{
					GeneratePlaylistEnabled = true;
				}
			}
		}

		private const string tabName = "CamScript Box";
		private const string resourceMenuBsml = "CameraPlusMovementScriptBox.View.ModMenu.bsml";
		private readonly SiraLog? logger;
		private MenuHost? host;
		private PluginConfig config;

		public ModMenuViewController(
			SiraLog? logger,
			PluginConfig config,
			[InjectOptional]PlaylistCreator? playlistCreator)
		{
			this.logger = logger;
			this.config = config;
			host = new(logger, config, playlistCreator);
			config.Reloaded += host.ModelChanged;
		}

		public void Initialize()
		{
			BSMLSettings.Instance.AddSettingsMenu(tabName, resourceMenuBsml, host);
		}

		public void Dispose()
		{
			if (host != null)
			{
				config.Reloaded -= host.ModelChanged;
				BSMLSettings.Instance.RemoveSettingsMenu(host);
				host = null;
			}
		}

		public void Tick()
		{
			host?.Tick();
		}
	}
}
