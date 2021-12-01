using QTRHack.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.UI
{
	public static class HackContext
	{
		public const string DEBUG_VERSION = "VNL-1.4.3.2";
		public const string PATH_CORES = "./Cores";
		public static HackKernel HackKernel
		{
			get;
			set;
		}
	}
}
