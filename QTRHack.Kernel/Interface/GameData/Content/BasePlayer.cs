using QHackLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData.Content
{
	public abstract class BasePlayer : GameObject
	{
		protected BasePlayer(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public virtual int Life
		{
			get => InternalObject.statLife;
			set => InternalObject.statLife = value;
		}

		public virtual GameObjectArray<BaseItem> Inventory
		{
			get => new GameObjectArray<BaseItem>(Core, InternalObject.inventory);
			set => InternalObject.inventory = value.InternalObject;
		}
	}
}
