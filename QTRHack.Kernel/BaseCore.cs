using QHackLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	public abstract class BaseCore
	{
		public abstract CoreVersionSig VersionSig
		{
			get;
		}
		public abstract Version KernelMinimum
		{
			get;
		}
		public GameContext GameContext
		{
			get;
		}
		public BaseCore(GameContext ctx)
		{
			GameContext = ctx;
		}

		/// <summary>
		/// <para>During the first phase</para>
		/// <para>To choose a core that work for this game instance</para>
		/// <para>Once one Core got matched, it will be chosen and others will be ignored</para>
		/// </summary>
		/// <returns>true if matches, otherwise false</returns>
		public abstract bool MatchGame();

		/// <summary>
		/// <para>Initialization phase</para>
		/// <para>Only the first core that matches the game instance will go into this phase</para>
		/// </summary>
		public abstract void Initialize();
	}
}
