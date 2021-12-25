using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader.Hooks
{
	public delegate bool Delegate_Main_General_Pre(Terraria.Main __instance);
	public delegate void Delegate_Main_General_Post(Terraria.Main __instance);

	public static class PHooks
	{
		public static UpdateHook Hook_Main_Update = new UpdateHook();
		public static SetupDrawInterfaceLayersHook Hook_Main_DrawInterfaceLayersHook = new SetupDrawInterfaceLayersHook();
	}
}
