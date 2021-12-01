using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	public sealed class HookOnAddress : RemoteHook
	{
		public int Address { get; }
		public bool ExecuteRawCode { get; }
		public int CodeSize { get; }
		public HookOnAddress(GameContext ctx, AssemblyCode code, int address, bool exeRaw = false, int size = 1024) : base(ctx, code)
		{
			Address = address;
			ExecuteRawCode = exeRaw;
			CodeSize = size;
		}

		public override void Attach()
		{
			InlineHook.Hook(GameContext.ProcessContext, Code, Address, false, ExecuteRawCode, CodeSize);
		}

		public override void Dispose()
		{
			InlineHook.FreeHook(GameContext.ProcessContext, Address);
		}
	}
}
