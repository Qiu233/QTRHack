using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	public abstract class GDAccess<T> where T : GDAccessArgs
	{
		public V Request<V>(GDRequest<T> request)
		{
			string methodName = $"OR_{request.Mode}";
			Type type = GetType();
			MethodInfo method = type.GetMethod(methodName);
			if (method == null)
				throw new HackKernelException($"Cannot find method: {methodName} in class: {type.FullName}");
			return (V)method.Invoke(this, new object[] { request.Args });
		}
	}
}
