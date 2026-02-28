using CameraPlus;
using CameraPlus.Behaviours;
using CameraPlusMovementScriptBox.Core;
using SiraUtil.Affinity;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlusMovementScriptBox.HarmonyPatches
{
	internal class CameraPlusControllerPatch : IAffinity
	{
		private readonly SiraLog? logger;
		private readonly SongMenuOption option;
		private readonly Action<string> customLevelPathSetter;

		public CameraPlusControllerPatch(
			SiraLog? logger,
			SongMenuOption option)
		{
			this.logger = logger;
			this.option = option;
			customLevelPathSetter = CreateCustomLevelPathSetter();
		}

		[AffinityPatch(typeof(CameraPlusController), nameof(CameraPlusController.OnActiveSceneChanged))]
		[AffinityPrefix]
		public void OnActiveSceneChangedPrefix()
		{
			if (!option.Active || option.Selected == null)
			{
				return;
			}
			logger?.Info($"Injecting the movement script to CameraPlus: {option.Selected.ScriptPath}");
			customLevelPathSetter.Invoke(option.Selected.ScriptPath);
		}

		private Action<string> CreateCustomLevelPathSetter()
		{
			try
			{
				var asm = typeof(CameraPlusBehaviour).Assembly;
				var songScriptBeatmapPatchType = asm.GetType("CameraPlus.HarmonyPatches.SongScriptBeatmapPatch", throwOnError: true);
				var field = songScriptBeatmapPatchType.GetField("customLevelPath", BindingFlags.Public | BindingFlags.Static);
				var valueParam = Expression.Parameter(typeof(string), "v");
				var fieldExpr = Expression.Field(null, field);
				var assignExpr = Expression.Assign(fieldExpr, valueParam);
				var lambda = Expression.Lambda<Action<string>>(assignExpr, valueParam);
				return lambda.Compile();
			}
			catch (Exception ex)
			{
				logger?.Error($"Failed to create custom level path setter. Make sure CameraPlus version is compatible with this mod.\n{ex}");
				return _ => { };
			}
		}
	}
}
