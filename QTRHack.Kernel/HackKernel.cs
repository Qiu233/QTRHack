using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	public class HackKernel
	{
		public GameContext Context { get; }
		public const string PLOADER_FILE = "PatchLoader.dll";

		public HackKernel(GameContext ctx)
		{
			Context = ctx;
		}

		public void LoadPatches()
		{
			if (!Context.LoadAssembly(Path.GetFullPath(PLOADER_FILE), "PatchLoader.PBase").Result)
				Console.WriteLine($"Failed: {PLOADER_FILE}");
		}
	}
}
