using QHackLib;
using QTRHack.Kernel.Interface.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface
{
	public abstract class BaseCore
	{
		public abstract CoreVersionSig VersionSig { get; }
		public abstract Version KernelMinimum { get; }
		public GameContext GameContext { get; private set; }
		public abstract IGameDataProvider GameDataProvider { get; }

		public BaseCore()
		{
		}

		/// <summary>
		/// Initialization phase.<br/>
		/// Only the first core that matches the game instance will go into this phase.
		/// </summary>
		public virtual void Initialize(GameContext ctx)
		{
			GameContext = ctx;
		}
	}
}
