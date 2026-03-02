using IPA.Logging;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace CameraPlusMovementScriptBox.Core
{
	internal class AdditionalInformation : IInitializable
	{
		private Logger? logger;
		public Impl? Interface { get; private set; }

		public AdditionalInformation(SiraLog? logger)
		{
			this.logger = logger?.Logger;
		}

		public void Initialize()
		{
			Interface = new(logger?.GetChildLogger(nameof(Impl)));
		}

		internal class Impl
		{
			private Logger? logger;
			private readonly Action<string>? ActionCamScript;

			public Impl(Logger? logger)
			{
				this.logger = logger;
				var className = "StandaloneBeatmapInformation.Core.AdditionalInformationInterface,StandaloneBeatmapInformation";
				var classType = Type.GetType(
					className,
					(name) => AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.FullName.Contains(name.FullName)),
					null);
				if (classType is null)
				{
					logger?.Warn($"Class ({className}) not found. Additional information will not work.");
					return;
				}

				ActionCamScript = SetDelegate(classType, "CamScriptAuthor");
			}

			private Action<string>? SetDelegate(Type classType, string memberName)
			{
				var value = Expression.Parameter(typeof(string), "value");
				var prop = classType.GetProperty(memberName);
				if (prop == null)
				{
					logger?.Warn($"Property ({memberName}) not found in {classType.FullName}. Additional information will not work.");
					return null;
				}
				var left = Expression.Property(null, prop);

				var lambda = Expression.Lambda<Action<string>>(
					Expression.Assign(left, value),
					value);

				return lambda.Compile();
			}

			public void SetCamScriptAuthor(string authorText)
			{
				ActionCamScript?.Invoke(authorText);
			}
		}
	}
}
