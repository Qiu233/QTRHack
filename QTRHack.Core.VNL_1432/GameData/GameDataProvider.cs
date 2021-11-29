using QTRHack.Kernel.Interface.GameData;
using QTRHack.Kernel.Interface.GameData.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.VNL_1432.GameData
{
	public class GameDataProvider : IGameDataProvider
	{
		public BaseItemAccess ItemAccess => new ItemAccess();
		public BasePlayerAccess PlayerAccess => new PlayerAccess();
	}
}
