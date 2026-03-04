using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.GameplaySetup;
using CameraPlusMovementScriptBox.Core;
using CameraPlusMovementScriptBox.Core.ComponentModel;
using CameraPlusMovementScriptBox.NonView;
using HMUI;
using IPA.Logging;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace CameraPlusMovementScriptBox.View
{
	internal class SongMenuViewController : IInitializable, IDisposable, ITickable
	{
		private readonly SiraLog? logger;
		private readonly SongMenuEvent songMenuEvent;
		private SongMenuHost? host;

		private const string tabName = "CamScr Box";
		private const string resourceMenuBsml = "CameraPlusMovementScriptBox.View.SongMenu.bsml";

		public SongMenuViewController(
			SiraLog? logger,
			SongMenuOption option,
			SongMenuEvent songMenuEvent,
			MovementScriptLoader loader,
			AdditionalInformation additionalInformation)
		{
			this.logger = logger;
			this.songMenuEvent = songMenuEvent;

			host = new(
				logger?.Logger?.GetChildLogger("Host"),
				option,
				loader,
				additionalInformation);
			songMenuEvent.SongSelectionChanged += OnSongSelectionChanged;
		}

		public void Initialize()
		{
			GameplaySetup.Instance.AddTab(tabName, resourceMenuBsml, host);
		}

		public void Dispose()
		{
			songMenuEvent.SongSelectionChanged -= OnSongSelectionChanged;
			GameplaySetup.Instance.RemoveTab(tabName);
			host?.Dispose();
		}

		public void Tick()
		{
			host?.Tick();
		}

		private void OnSongSelectionChanged(SongMenuEvent.SongSelectionEventArgs args)
		{
			host?.OnSongSelectionChanged(args);
		}

		internal class ScriptElement
		{
			[UIValue("script-text")]
			public string ScriptText
			{
				get
				{
					var title = string.IsNullOrEmpty(ScriptDef?.Title) ? "(---)" : ScriptDef!.Title;
					return $"[{ScriptDef?.Author}] {title}";
				}
			}

			[UIValue("hover-hint")]
			public string HoverHint
			{
				get
				{
					var fType = ScriptDef?.IsDirectFile == true ? "File" : "Folder";
					return $"({fType}) {ScriptDef?.BaseName}";
				}
			}

			public MovementScriptDef? ScriptDef { get; set; }
		}

		internal class SongMenuHost : HostBase, IDisposable
		{
			private readonly Logger? logger;
			private readonly SongMenuOption option;
			private readonly MovementScriptLoader loader;
			private readonly AdditionalInformation additional;

			private (string? bsr, string? hash)? lastSong;
			private readonly Dictionary<string, string> lastSelectedScriptMap = new();

			public SongMenuHost(
				Logger? logger,
				SongMenuOption option,
				MovementScriptLoader loader,
				AdditionalInformation additional
			)
			{
				this.logger = logger;
				this.option = option;
				this.loader = loader;
				this.additional = additional;

				ActionOnModelChanged();
				option.PropertyChanged += ModelChanged;
			}

			public void Dispose()
			{
				option.PropertyChanged -= ModelChanged;
			}

			public override void ActionOnModelChanged()
			{
				Active = option.Active;
			}

			private bool active;
			[UIValue("script-active")]
			public bool Active
			{
				get => active;
				set => SetProperty(ref active, value, () => { option.Active = value; OnActiveChanged(); });
			}

			private string? scriptTitle;
			[UIValue("script-title")]
			public string? ScriptTitle
			{
				get => scriptTitle;
				set => SetProperty(ref scriptTitle, value);
			}

			private string? baseInfo;
			[UIValue("base-info")]
			public string? BaseInfo
			{
				get => baseInfo;
				set => SetProperty(ref baseInfo, value);
			}

			private string? scriptAuthor;
			[UIValue("script-author")]
			public string? ScriptAuthor
			{
				get => scriptAuthor;
				set => SetProperty(ref scriptAuthor, value);
			}

			private string? fileType;
			[UIValue("file-type")]
			public string? FileType
			{
				get => fileType;
				set => SetProperty(ref fileType, value);
			}

			private string? description;
			[UIValue("description")]
			public string? Description
			{
				get => description;
				set => SetProperty(ref description, value);
			}

			private void OnActiveChanged() => _UpdateAdditionalText();

			public void OnSongSelectionChanged(SongMenuEvent.SongSelectionEventArgs args)
			{
				var currentSong = (args.bsr, args.hash);
				if (lastSong == currentSong)
				{
					return;
				}
				lastSong = currentSong;

				var scriptsB = loader.GetScriptsByBsr(args.bsr);
				var scriptsH = loader.GetScriptsBySongHash(args.hash);
				var scriptsObj = scriptsB
					.Union(scriptsH)
					.Select(x => new ScriptElement()
					{
						ScriptDef = x
					});
				ScriptsClear();
				ScriptsAddRange(scriptsObj);

				bool selected = false;
				var lastPath = _FindLastSelectedScript(args.bsr, args.hash);
				if (lastPath != null)
				{
					var candidate = scriptsObj.FirstOrDefault(s => s.ScriptDef?.ScriptPath == lastPath)?.ScriptDef;
					if (candidate != null)
					{
						selectedScript = candidate;
						_ScriptSelectedUpdate();
						var idx = Scripts.IndexOf(Scripts.FirstOrDefault(s => (s as ScriptElement)?.ScriptDef == candidate));
						if (idx >= 0)
						{
							ScriptTable?.TableView?.SelectCellWithIdx(idx);
							selected = true;
						}
					}
				}
				if (!selected)
				{
					// select the first one by default.
					selectedScript = scriptsObj.FirstOrDefault()?.ScriptDef;
					_ScriptSelectedUpdate();
					if (selectedScript != null)
					{
						ScriptTable?.TableView?.SelectCellWithIdx(0);
					}
				}
			}

			[UIComponent("script-list")]
			public readonly CustomCellListTableData? ScriptTable;

			[UIValue("scripts")]
			public List<object> Scripts { get; } = new();
			protected void ScriptsAddRange(IEnumerable<ScriptElement> scripts)
			{
				Scripts.InsertRange(0, scripts);
				ScriptTable?.TableView?.ReloadData();
				_RefreshState();
			}

			protected void ScriptsClear()
			{
				selectedScript = null;
				_ScriptSelectedUpdate();
				Scripts.Clear();
				ScriptTable?.TableView?.ReloadData();
				ScriptTable?.TableView?.ClearSelection();
			}

			MovementScriptDef? selectedScript;

			[UIAction("script-selected")]
			public void ScriptSelected(TableView _, object? row)
			{
				var selected = (row as ScriptElement)?.ScriptDef;
				if (selected == selectedScript)
				{
					return;
				}
				selectedScript = selected;
				_ScriptSelectedUpdate();
				_UpdateLastSelectedScript();
			}

			private void _ScriptSelectedUpdate()
			{
				if (selectedScript == null)
				{
					ScriptAuthor = null;
					ScriptTitle = null;
					BaseInfo = null;
					Description = null;
				}
				else
				{
					ScriptAuthor = $"[{selectedScript?.Author}]";
					ScriptTitle = string.IsNullOrEmpty(selectedScript?.Title) ? "(---)" : selectedScript?.Title;
					var fileType = selectedScript != null
						? (selectedScript.IsDirectFile ? "File" : "Folder")
						: null;
					BaseInfo = $"({fileType}) {selectedScript?.BaseName}";
					Description = selectedScript?.Description;
				}

				option.Selected = selectedScript;
				_UpdateAdditionalText();
			}

			private void _UpdateLastSelectedScript()
			{
				if (selectedScript == null)
				{
					return;
				}

				var key = selectedScript.Bsr ?? selectedScript.SongHash;
				if (key == null)
				{
					return;
				}

				lastSelectedScriptMap[key] = selectedScript.ScriptPath;
			}

			private void _UpdateAdditionalText()
			{
				string additionalText = string.Empty;
				if (option.Active && option.Selected != null)
				{
					var author = string.IsNullOrWhiteSpace(selectedScript?.Author)
						? "--"
						: selectedScript?.Author;
					additionalText = $"Camera Script by [{author}]";
				}
				additional.Interface?.SetCamScriptAuthor(additionalText);
			}

			private string? _FindLastSelectedScript(string? bsr, string? hash)
			{
				if (bsr != null && lastSelectedScriptMap.TryGetValue(bsr, out var lastPathByBsr))
				{
					return lastPathByBsr;
				}
				if (hash != null && lastSelectedScriptMap.TryGetValue(hash, out var lastPathByHash))
				{
					return lastPathByHash;
				}
				return null;
			}

			private void _RefreshState()
			{
				_ScriptSelectedUpdate();
				RaisePropertyChanged();
			}
		}
	}
}
