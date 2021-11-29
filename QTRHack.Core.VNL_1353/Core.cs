using QTRHack.Core.VNL_1353.GameData;
using QTRHack.Kernel;
using QTRHack.Kernel.Interface;
using QTRHack.Kernel.Interface.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.VNL_1353
{
	public class Core : DefaultCore
	{
		public Core(GameContext ctx) : base(ctx)
		{
		}

		public override CoreVersionSig VersionSig => new CoreVersionSig(GameType.VNL, Version.Parse("1.3.5.3"), 0);
		public override Version KernelMinimum => Version.Parse("1.0.0.0");

		public override IGameDataProvider GameDataProvider => new GameDataProvider();


		public override void Initialize()
		{
		}

	}
}
