using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Settings;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraPlusMovementScriptBox.Configuration;
using CameraPlusMovementScriptBox.Core.ComponentModel;
using Zenject;

namespace CameraPlusMovementScriptBox.View
{
	internal class ModMenuViewController : IInitializable, IDisposable, ITickable
	{
		internal class MenuHost : HostBase
		{
			private readonly SiraLog? logger;
			private readonly PluginConfig config;

			private bool _Enabled;
			[UIValue("enabled")]
			public bool Enabled
			{
				get => _Enabled;
				set => SetProperty(ref _Enabled, value, () => config.Enabled = value);
			}

			public override void ActionOnModelChanged()
            {
				Enabled = config.Enabled;
			}

			public MenuHost(
				SiraLog? logger,
				PluginConfig config)
			{
				this.logger = logger;
				this.config = config;
				ActionOnModelChanged();
			}
		}

		private const string tabName = "CamScript Box";
		private const string resourceMenuBsml = "CameraPlusMovementScriptBox.View.ModMenu.bsml";
		private readonly SiraLog? logger;
		private MenuHost? host;
		private PluginConfig config;

		public ModMenuViewController(
			SiraLog? logger,
			PluginConfig config)
		{
			this.logger = logger;
			this.config = config;
			host = new(logger, config);
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
