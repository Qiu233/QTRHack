using Microsoft.Diagnostics.Runtime;
using QHackLib;
using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using QTRHack.Kernel;
using QTRHack.Kernel.Interface.GameData.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QTRHack.Test
{
	class Program
	{
		static void Main(string[] args)
		{
			using (HackKernel kernel = HackKernel.Create(Process.GetProcessesByName("Terraria")[0]))
			{
				/*dynamic plr = kernel.GameContext.GetStaticGameObject("Terraria.Main", "player")[0];
				AssemblyCode code = plr.inventory[0].SetDefaults("Terraria.Item.SetDefaults(Int32)").Call(true, null, 3063);
				kernel.GameContext.RunOnManagedThread(code).Dispose();*/
				var item = kernel.GameContext.GetStaticGameObject("Terraria.Main", "player")[0].inventory[0];
				int life = kernel.RequestGD<BaseItemAccess.ItemAccessArgs, int>(
					"Type",
					new BaseItemAccess.ItemAccessArgs(kernel.GameContext)
					{
						Item = item
					});
				Console.WriteLine(life);
			}
		}
	}
}
