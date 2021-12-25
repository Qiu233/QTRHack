using PatchLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputFix
{
	public class InputFixPatch : BasePatch
	{
		public override string Name => "InputFix";
		public override Version Version => Version.Parse("1.0.0.0");
		public override string Description => "A fix for backspacing when inputing unicode";
		public override bool HasRawPatch => true;
	}
}
