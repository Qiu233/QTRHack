using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	public class GDRequest<T> where T : GDAccessArgs
	{
		public T Args { get; set; }
		public string Mode { get; set; }
	}
}
