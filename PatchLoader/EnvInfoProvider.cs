using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader
{
	public static class EnvInfoProvider
	{
		public const string DIR_Patches = "Patches";
		public const string VersionSig = "VNL-1.4.3.2";

		public static string GetPatchesDirectory() 
			=> Path.Combine(Path.GetFullPath(Assembly.GetExecutingAssembly().Location), DIR_Patches);
	}
}
