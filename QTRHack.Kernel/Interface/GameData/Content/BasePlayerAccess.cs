using QHackLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData.Content
{
	public abstract class BasePlayerAccess :
		GDAccess<BasePlayerAccess.PlayerAccessArgs>
	{
		/// <summary>
		/// To get icon of an item object.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public abstract GameObject OR_Life(PlayerAccessArgs arg);

		public class PlayerAccessArgs : GDAccessArgs
		{
			public PlayerAccessArgs(GameContext gameContext) : base(gameContext)
			{
			}

			public GameObject Player { get; set; }
		}
	}
}
