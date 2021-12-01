using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	/// <summary>
	/// Represents a permanent hook.
	/// </summary>
	public abstract class RemoteHook : IRemoteExecution, IDisposable
	{
		public GameContext GameContext { get; }
		public AssemblyCode Code { get; }
		protected RemoteHook(GameContext ctx, AssemblyCode code)
		{
			GameContext = ctx;
			Code = code;
		}

		public abstract void Attach();
		public abstract void Dispose();
	}
}
