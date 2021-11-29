using QHackLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData.Content
{
	public abstract class BaseItemAccess : GDAccess<BaseItemAccess.ItemAccessArgs>
	{
		/// <summary>
		/// To get icon of an item object.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public abstract Image OR_Icon(ItemAccessArgs arg);

		public abstract object OR_Type(ItemAccessArgs arg);

		public class ItemAccessArgs : GDAccessArgs
		{
			public ItemAccessArgs(GameContext gameContext) : base(gameContext)
			{
			}

			public GameObject Item { get; set; }
		}
	}
}
