using Microsoft.Diagnostics.Runtime;
using QHackLib;
using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using QTRHack.Kernel;
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
				int handle = kernel.GameContext.ProcessContext.Handle;
				dynamic plr = kernel.GameContext.GetStaticGameObject("Terraria.Main", "player")[0];
				dynamic pos = plr.position;
				AssemblyCode code = AssemblySnippet.FromCode(new AssemblyCode[] {
					//kernel.GameContext.GetStaticMethodByName("Terraria.NPC","NewNPC").Call(true,null,(int)(float)pos.X,(int)(float)pos.Y,50,0,0f,0f,0f,0f,255),
					plr.inventory[0].SetDefaults("Terraria.Item.SetDefaults(Int32)").Call(true,null,3063),
				});

				RemoteExecution re = RemoteExecution.Create(kernel.GameContext.ProcessContext, code);
				int addr_type_str = kernel.GameContext.ProcessContext.DataAccess.NewWCHARArray("System.Action");

				InlineHook.InjectAndWait(
					kernel.GameContext.ProcessContext,
					AssemblySnippet.StartManagedThread(
						kernel.GameContext.ProcessContext,
						re.CodeAddress,
						addr_type_str),
					kernel.GameContext.GameAddressHelper.
					GetFunctionAddress("Terraria.Main", "DoUpdate"), true);
				kernel.GameContext.ProcessContext.DataAccess.FreeMemory(addr_type_str);
				re.Dispose();
			}
		}
	}
}
