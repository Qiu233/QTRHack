using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader.Hooks
{
	[HarmonyPatch(typeof(Terraria.Main), "SetupDrawInterfaceLayers")]
	public class SetupDrawInterfaceLayersHook
	{
		public event Delegate_Main_General_Pre Pre;
		public event Delegate_Main_General_Post Post;

		[HarmonyPrefix]
		internal static bool Prefix(Terraria.Main __instance) => PHooks.Hook_Main_DrawInterfaceLayersHook.Pre(__instance);

		[HarmonyPostfix]
		internal static void Postfix(Terraria.Main __instance) => PHooks.Hook_Main_DrawInterfaceLayersHook.Post(__instance);
	}
}
