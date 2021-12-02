using QHackLib;
using QTRHack.Kernel.Interface;
using QTRHack.Kernel.Interface.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.GameObjects
{
	public abstract class BaseChest : GameObject
	{
		protected BaseChest(BaseCore core, HackObject obj) : base(core, obj)
		{
		}
	}
}
