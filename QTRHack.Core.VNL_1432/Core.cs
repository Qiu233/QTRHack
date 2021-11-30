using QTRHack.Core.VNL_1432.GameData;
using QTRHack.Kernel;
using QTRHack.Kernel.Interface;
using QTRHack.Kernel.Interface.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.VNL_1432
{
	public class Core : BaseCore
	{
		public Core() : base()
		{
		}

		public override CoreVersionSig VersionSig => new CoreVersionSig(GameType.VNL, Version.Parse("1.4.3.2"), 0);
		public override Version KernelMinimum => Version.Parse("1.0.0.0");

		public override IGameDataProvider GameDataProvider => new GameDataProvider();


		public override void Initialize(GameContext ctx)
		{
			base.Initialize(ctx);
		}

	}
}
