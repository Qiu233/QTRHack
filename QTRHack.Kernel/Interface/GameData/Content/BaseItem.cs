using QHackLib;
using QHackLib.Assemble;
using QTRHack.Kernel.RemoteExecution;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData.Content
{
	public abstract class BaseItem : GameObject
	{
		protected BaseItem(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public virtual object Icon
		{
			get => new NotImplementedException();
			set => new NotImplementedException();
		}
		public virtual int Type
		{
			get => InternalObject.type;
			set => InternalObject.type = value;
		}

		public virtual void SetDefaults(int type)
		{
			new ActionOnManagedThread(Core.GameContext, 
				InternalObject.SetDefaults("Terraria.Item.SetDefaults(Int32)").Call(true, null, type)).Execute();
		}
	}
}
