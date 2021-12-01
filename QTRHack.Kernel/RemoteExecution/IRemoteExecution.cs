using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.RemoteExecution
{
	public interface IRemoteExecution
	{
		GameContext GameContext { get; }
		AssemblyCode Code { get; }
	}
}
