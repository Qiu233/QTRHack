using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	/// <summary>
	/// Represents a snippet of code that will run for a short time before it returns, including neither permanent hooks nor aob-replacements.<br/>
	/// Note that there are multiple potential implementations to run the code.
	/// </summary>
	public abstract class RemoteAction : IRemoteExecution
	{
		public GameContext GameContext { get; }
		public AssemblyCode Code { get; }
		protected RemoteAction(GameContext ctx, AssemblyCode code)
		{
			GameContext = ctx;
			Code = code;
		}

		public abstract void Execute();
	}
}
