using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	public abstract class GDAccessArgs
	{
		public GameContext GameContext
		{
			get;
		}

		protected GDAccessArgs(GameContext gameContext)
		{
			GameContext = gameContext;
		}
	}
}
