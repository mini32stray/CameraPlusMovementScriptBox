using IPA.Logging;
using IPA.Utilities;
using Newtonsoft.Json;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using IPALogger = IPA.Logging.Logger;

namespace CameraPlusMovementScriptBox.Core
{
	internal class MovementScriptLoader
	{
		internal class DirectFileParser
		{
			private Regex bsrFilePattern;

			public DirectFileParser()
			{
				bsrFilePattern = new(@"^(?<bsr>[0-9a-fA-F]{1,6})\s+(\(.+?\)){0,1}(?<title>.*)\[(?<author>.+?)\]\.json$");
			}

			public MovementScriptDef? Parse(string filePath)
			{
				var fileName = Path.GetFileName(filePath);
				var match = bsrFilePattern.Match(fileName);
				if (!match.Success)
				{
					return null;
				}
				var title = GetGroup(match, "title", true) ?? "No title";
				var bsr = (GetGroup(match, "bsr", false) ?? "0").ToLower();
				var author = GetGroup(match, "author", true);
				return new(
					Title: title,
					BaseName: fileName,
					ScriptPath: filePath,
					IsDirectFile: true,
					Bsr: bsr,
					SongHash: null,
					Author: author,
					Description: null);
			}

			private string? GetGroup(Match match, string groupName, bool trim)
			{
				var group = match.Groups[groupName];
				if (group.Success)
				{
					var value = group.Value;
					if (trim)
					{
						value = value.Trim();
					}
					return value;
				}
				return null;
			}
		}

		internal class DirectoryParser
		{
			private readonly IPALogger? logger;

			public DirectoryParser(IPALogger? logger)
			{
				this.logger = logger;
			}

			private class MovementScriptInfo
			{
				public string? Title { get; set; }
				public string? Bsr { get; set; }
				public string? SongHash { get; set; }
				public string? Author { get; set; }
				public string? Description { get; set; }
			}

			public MovementScriptDef? Parse(string infoContent, string scriptPath)
			{
				if (infoContent == null)
				{
					return null;
				}

				var info = ParseInfo(infoContent);
				if (info == null)
				{
					return null;
				}

				var title = info.Title ?? "No title";
				var bsr = string.IsNullOrEmpty(info.Bsr) ? null : info.Bsr!.Trim();
				var songHash = string.IsNullOrEmpty(info.SongHash) ? null : info.SongHash!.Trim();
				if (bsr == null && songHash == null)
				{
					logger?.Error($"info.json does not contain a valid BSR or SongHash. Skip loading this script.");
					return null;
				}

				return new MovementScriptDef(
					Title: title,
					BaseName: Path.GetFileName(Path.GetDirectoryName(scriptPath)),
					ScriptPath: scriptPath,
					IsDirectFile: false,
					Bsr: bsr,
					SongHash: songHash,
					Author: info.Author,
					Description: info.Description);
			}

			private MovementScriptInfo? ParseInfo(string infoContent)
			{
				try
				{
					var info = JsonConvert.DeserializeObject<MovementScriptInfo>(infoContent);
					if (info == null)
					{
						return null;
					}
					return info;
				}
				catch
				{
					logger?.Error("Failed to parse info.json. Ensure it is a valid JSON file with the correct structure.");
					return null;
				}
			}
		}

		private readonly SiraLog? logger;
		private readonly SimpleMultiMap<string, MovementScriptDef> loadedScriptsByBsr;
		private readonly SimpleMultiMap<string, MovementScriptDef> loadedScriptsBySongHash;

		private readonly DirectFileParser directFileParser;
		private readonly DirectoryParser directoryParser;

		public string BaseDirectoryPath { get; }

		public MovementScriptLoader(
			SiraLog? logger)
		{
			this.logger = logger;
			BaseDirectoryPath = Path.Combine(UnityGame.UserDataPath, "CameraPlusMovementScriptBox");
			loadedScriptsByBsr = new();
			loadedScriptsBySongHash = new();

			directFileParser = new();
			directoryParser = new(logger?.Logger?.GetChildLogger(nameof(DirectoryParser)));
		}

		public void Load()
		{
			logger?.Info($"Start loading movement scripts from '{BaseDirectoryPath}'...");
			var startTime = DateTime.Now;

			loadedScriptsByBsr.Clear();
			if (!Directory.Exists(BaseDirectoryPath))
			{
				logger?.Error($"Base directory '{BaseDirectoryPath}' does not exist. Skip loading scripts.");
				return;
			}

			var files = Directory.GetFiles(BaseDirectoryPath, "*.json");
			foreach (var file in files)
			{
				var fileName = Path.GetFileName(file);
				var def = directFileParser.Parse(file);
				if (def == null)
				{
					logger?.Warn($"Not a valid song script format: '{fileName}'. Skip loading this file.");
					continue;
				}
				if (def.Bsr == null)
				{
					logger?.Error($"File '{fileName}' does not contain a valid BSR.");
					continue;
				}
				loadedScriptsByBsr.Add(def.Bsr, def);
			}

			loadedScriptsBySongHash.Clear();
			var directories = Directory.GetDirectories(BaseDirectoryPath);
			foreach (var dir in directories)
			{
				var dirInfo = new DirectoryInfo(dir);
				var infoFilePath = Path.Combine(dir, "info.json");
				var scriptFilePath = Path.Combine(dir, "SongScript.json");
				if (!(File.Exists(infoFilePath) && File.Exists(scriptFilePath)))
				{
					logger?.Warn($"Directory '{dir}' does not contain both 'info.json' and 'SongScript.json'. Skip loading this directory.");
					continue;
				}
				var def = directoryParser.Parse(File.ReadAllText(infoFilePath), scriptFilePath);
				if (def == null)
				{
					logger?.Warn($"Failed to parse info.json in directory '{dir}'. Skip loading this directory.");
					continue;
				}
				if (def.Bsr != null)
				{
					loadedScriptsByBsr.Add(def.Bsr, def);
				}
				if (def.SongHash != null)
				{
					loadedScriptsBySongHash.Add(def.SongHash, def);
				}
			}

			var duration = DateTime.Now - startTime;
			logger?.Info($"Finished loading movement scripts in {duration.TotalMilliseconds:F2} ms. Total scripts loaded: {loadedScriptsByBsr.Count} + {loadedScriptsBySongHash.Count}.");
		}

		public IReadOnlyList<MovementScriptDef> GetScriptsByBsr(string? bsr)
		{
			if (bsr == null)
			{
				return Array.Empty<MovementScriptDef>();
			}
			return loadedScriptsByBsr.GetValues(bsr);
		}

		public IReadOnlyList<MovementScriptDef> GetScriptsBySongHash(string? songHash)
		{
			if (songHash == null)
			{
				return Array.Empty<MovementScriptDef>();
			}
			return loadedScriptsBySongHash.GetValues(songHash);
		}
	}
}
