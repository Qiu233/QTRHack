using QHackLib;
using QTRHack.Kernel.Interface.GameData.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.VNL_1353.GameData
{
	public class PlayerAccess : BasePlayerAccess
	{
		public override GameObject OR_Life(PlayerAccessArgs arg)
		{
			dynamic player = arg.Player;
			return player.statLife;
		}
	}
}
