using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria.UI;

namespace PatchLoader
{
	public abstract class BasePatch
	{
		public abstract string Name { get; }
		public abstract Version Version { get; }
		public abstract string Description { get; }
		/// <summary>
		/// Indicates whether this patch has raw patch using harmony.
		/// </summary>
		public abstract bool HasRawPatch { get; }

		public Assembly Assembly { get; internal set; }

		public BasePatch() { }

		public virtual void Load() { }
		public virtual void UnLoad() { }

		public virtual void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) { }
	}
}
