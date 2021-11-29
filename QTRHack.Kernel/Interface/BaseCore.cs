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
		public GameContext GameContext { get; }
		public abstract IGameDataProvider GameDataProvider { get; }

		public BaseCore(GameContext ctx)
		{
			GameContext = ctx;
		}

		/// <summary>
		/// During the first phase.<br/>
		/// To choose a core that work for this game instance.<br/>
		/// Once one Core got matched, it will be chosen and others will be ignored.
		/// </summary>
		/// <returns>true if matches, otherwise false</returns>
		public abstract bool MatchGame();

		/// <summary>
		/// Initialization phase.<br/>
		/// Only the first core that matches the game instance will go into this phase.
		/// </summary>
		public abstract void Initialize();
	}
}
