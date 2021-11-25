using Microsoft.Diagnostics.Runtime;
using QHackLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	/// <summary>
	/// One kernel to one game instance
	/// </summary>
	public class HackKernel : IDisposable
	{
		public GameContext GameContext
		{
			get;
		}
		private HackKernel(Process process)
		{
			GameContext = GameContext.OpenGame(process);
		}

		/// <summary>
		/// To attach to a game instance
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public static HackKernel Create(Process process)
		{
			return new HackKernel(process);
		}

		public void Dispose()
		{
			GameContext.Dispose();
		}
	}
}
