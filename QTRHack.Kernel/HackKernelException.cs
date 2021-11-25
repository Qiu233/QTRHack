using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	public class HackKernelException : Exception
	{
		public HackKernelException(string msg) : base(msg)
		{

		}
	}
}
