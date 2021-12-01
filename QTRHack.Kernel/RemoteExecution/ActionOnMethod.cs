using QHackLib;
using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class ActionOnMethod : RemoteAction
	{
		public HackMethod Method
		{
			get;
		}
		public ActionOnMethod(GameContext ctx, AssemblyCode code, HackMethod method) : base(ctx, code)
		{
			Method = method;
		}

		public override void Execute()
		{
			InlineHook.HookAndWait(GameContext.ProcessContext, Code, (int)Method.InternalClrMethod.NativeCode, true);
		}
	}
}
