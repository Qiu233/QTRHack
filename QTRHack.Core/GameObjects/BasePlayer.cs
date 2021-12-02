using QHackLib;
using QTRHack.Core.GameObjects;
using QTRHack.Kernel.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.GameObjects
{
	/// <summary>
	/// Wrapper for Terraria.Player
	/// </summary>
	public abstract partial class BasePlayer : BaseEntity
	{
		protected BasePlayer(BaseCore core, HackObject obj) : base(core, obj)
		{

		}
	}
}
