using QHackLib;
using QTRHack.Kernel.Interface;
using QTRHack.Kernel.RemoteExecution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.GameObjects
{
	/// <summary>
	/// Wrapper for Terraria.Item
	/// </summary>
	public abstract partial class BaseItem : BaseEntity
	{
		protected BaseItem(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public virtual void SetDefaults(int type)
		{
			new ActionOnManagedThread(Core.GameContext,
				TypedInternalObject.GetMethodCall("Terraria.Item.SetDefaults(Int32)").
				Call(true, null, type)).Execute();
		}
	}
}
