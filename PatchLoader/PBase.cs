using HarmonyLib;
using PatchLoader.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader
{
	internal class PBase
	{
		/// <summary>
		/// The entrypoint of PatchLoader
		/// </summary>
		public PBase()
		{
			new Harmony("PatchLoader").PatchAll();
			AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
		}

		private void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args)
		{
			new Harmony(args.LoadedAssembly.FullName).PatchAll(args.LoadedAssembly);
			foreach (var type in args.LoadedAssembly.DefinedTypes)
				foreach (var method in type.DeclaredMethods)
					RuntimeHelpers.PrepareMethod(method.MethodHandle);
		}
	}
}
