using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader
{
	public static class PrivateGetter
	{
		public static MethodInfo GetStaticMethod<T>(string name) => typeof(T).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
		public static FieldInfo GetStaticField<T>(string name) => typeof(T).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
	}
}
