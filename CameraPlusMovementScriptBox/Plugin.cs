using CameraPlusMovementScriptBox.Core;
using CameraPlusMovementScriptBox.HarmonyPatches;
using CameraPlusMovementScriptBox.NonView;
using CameraPlusMovementScriptBox.View;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace CameraPlusMovementScriptBox
{
	[Plugin(RuntimeOptions.DynamicInit), NoEnableDisable]
	public class Plugin
	{
		[Init]
		public void Init(IPALogger logger, Config conf, Zenjector zenjector)
		{
			var pluginConfig = conf.Generated<Configuration.PluginConfig>();
			zenjector.UseMetadataBinder<Plugin>();
			zenjector.UseLogger(logger);
			zenjector.UseHttpService();
			zenjector.Install(Location.App, container =>
			{
				container.BindInstance(pluginConfig);
				if (pluginConfig.Enabled)
				{
					container.BindInterfacesAndSelfTo<MovementScriptLoader>().AsSingle();
					container.BindInterfacesAndSelfTo<MovementScriptLoadingOperator>().AsSingle().NonLazy();
					container.BindInterfacesAndSelfTo<SongMenuOption>().AsSingle();
					container.BindInterfacesAndSelfTo<CameraPlusControllerPatch>().AsSingle().NonLazy();
					container.BindInterfacesAndSelfTo<AdditionalInformation>().AsSingle().NonLazy();
					container.BindInterfacesAndSelfTo<PlaylistCreator>().AsSingle();
				}
			});
			zenjector.Install(Location.Menu, container =>
			{
				container.BindInterfacesTo<ModMenuViewController>().AsSingle().NonLazy();
				if (pluginConfig.Enabled)
				{
					container.BindInterfacesTo<SongMenuViewController>().AsSingle();
					container.BindInterfacesAndSelfTo<SongMenuEvent>().AsSingle();
				}
			});
			zenjector.Install(Location.Player, container =>
			{
			});
		}
	}
}
