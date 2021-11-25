using QHackLib;
using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using QTRHack.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Test
{
	struct Vector2
	{
		public float X, Y;
	}
	class Program
	{
		static void Main(string[] args)
		{
			using (HackKernel kernel = HackKernel.Create(Process.GetProcessesByName("Terraria")[0]))
			{
				dynamic plr = kernel.GameContext.GetStaticGameObject("Terraria.Main", "player")[0];
				AssemblyCode code = plr.inventory[0].
					SetDefaults("Terraria.Item.SetDefaults(Int32)").Call(true, null, 3063);
				InlineHook.InjectAndWait(kernel.GameContext.ProcessContext, 
					code, kernel.GameContext.GameAddressHelper.GetFunctionAddress("Terraria.Main", "DoUpdate"), true);
			}
		}
	}
}
