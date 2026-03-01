using CameraPlusMovementScriptBox.Core.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraPlusMovementScriptBox.Core
{
	internal class SongMenuOption : BindableBase
	{
		private bool _active = true;
		public bool Active
		{
			get => _active;
			set => SetProperty(ref _active, value);
		}

		private MovementScriptDef? _selected;
		public MovementScriptDef? Selected
		{
			get => _selected;
			set => SetProperty(ref _selected, value);
		}
	}
}
