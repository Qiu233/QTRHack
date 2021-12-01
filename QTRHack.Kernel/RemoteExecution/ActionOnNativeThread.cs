using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	public sealed class ActionOnNativeThread : RemoteAction
	{
		public ActionOnNativeThread(GameContext ctx, AssemblyCode code) : base(ctx, code)
		{
		}

		public override void Execute()
		{
			using (RemoteThread rt = RemoteThread.Create(GameContext.ProcessContext, Code))
			{
				rt.RunOnNativeThread();
			}
		}
	}
}
