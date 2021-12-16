using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public sealed class GameObjectNameAttribute : Attribute
	{
		/// <summary>
		/// Should be the full name<br/>
		/// i.e. System.Int32
		/// </summary>
		public string TypeName;

		/// <summary>
		/// 
		/// </summary>
		public string AssemblyName;
	}
}
