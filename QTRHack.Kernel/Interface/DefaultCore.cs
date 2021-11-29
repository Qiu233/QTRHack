using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface
{
	/// <summary>
	/// <para>Do not inherit this class if the core is for OTHER types of game rather than TML and VNL</para>
	/// <para>Instead you should verify the game instance by overriding the method MatchGame of class BaseCore</para>
	/// </summary>
	public abstract class DefaultCore : BaseCore
	{
		public DefaultCore(GameContext ctx) : base(ctx)
		{
		}

		/// <summary>
		/// <para>Default match using CoreVersionSig</para>
		/// <para>Only tells TML from VNL</para>
		/// </summary>
		/// <returns></returns>
		public override bool MatchGame()
		{
			GameType gameType = VersionSig.GameType;
			Version gameVersion = VersionSig.GameVersion;

			if (gameVersion.CompareTo(GameContext.GameAssemblyName.Version) != 0)
				return false;
			bool TML = GameContext.ProcessContext.MainAddressHelper.Module.GetTypeByName("Terraria.ModLoader.ModLoader") != null;
			return (gameType == GameType.TML && TML) || (!TML && gameType == GameType.VNL);
		}
	}
}
