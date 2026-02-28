using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace CameraPlusMovementScriptBox.Core.ComponentModel
{
	internal abstract class HostBase : BindableBase, ITickable
    {
		private bool modelChanged;

		public void ModelChanged()
        {
			modelChanged = true;
        }

		public void ModelChanged(object sender, PropertyChangedEventArgs e) => ModelChanged();

		public void Tick()
        {
			if (!modelChanged)
			{
				return;
			}
			modelChanged = false;
			ActionOnModelChanged();
        }

		public abstract void ActionOnModelChanged();
    }
}
