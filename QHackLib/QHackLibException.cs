using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	public class QHackLibException : Exception
	{
		public QHackLibException(string msg) : base(msg)
		{

		}
	}
}
