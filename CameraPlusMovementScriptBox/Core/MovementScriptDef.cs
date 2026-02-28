using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlusMovementScriptBox.Core
{
	internal record MovementScriptDef(
		string Title,
		string BaseName,
		string ScriptPath,
		bool IsDirectFile,
		string? Bsr,
		string? SongHash,
		string? Author,
		string? Description);
}
