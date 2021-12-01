using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	public sealed class ActionOnManagedThread : RemoteAction
	{
		public ActionOnManagedThread(GameContext ctx, AssemblyCode code) : base(ctx, code)
		{
		}
		/// <summary>
		/// Still some problem with this.
		/// </summary>
		public override void Execute()
		{
			GameContext.RunOnManagedThread(Code).Dispose();
		}
	}
}
