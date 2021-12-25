using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader
{
	public class PatchLoader
	{
		internal readonly static List<BasePatch> Patches = new List<BasePatch>();
		private static BasePatch LoadPatchFile(string file)
		{
			Assembly asm = Assembly.LoadFrom(file);
			var types = asm.DefinedTypes.Where(t => t.IsSubclassOf(typeof(BasePatch))).ToArray();
			if (types.Length == 0 || types.Length >= 2)
				return null;
			var obj = types[0].GetConstructor(Type.EmptyTypes).Invoke(null) as BasePatch;
			obj.Assembly = asm;
			return obj;
		}

		public static void LoadAllPatchFiles()
		{
			Patches.Clear();
			foreach (var file in Directory.EnumerateFiles(EnvInfoProvider.GetPatchesDirectory(), "*.dll"))
			{
				var patch = LoadPatchFile(file);
				if (patch is null)
					continue;
				Patches.Add(patch);
			}
		}

		public static void LoadAllPatches()
		{
			LoadAllPatchFiles();
			foreach (var patch in Patches)
			{
				patch.Load();
			}
		}
	}
}
