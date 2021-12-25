using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PatchLoader.Hooks
{
	[HarmonyPatch(typeof(Terraria.Main), "Update")]
	public class UpdateHook
	{
		public delegate bool PreUpdateHookDelegate(Terraria.Main __instance, ref GameTime gameTime);
		public delegate void PostUpdateHookDelegate(Terraria.Main __instance, ref GameTime gameTime);

		public event PreUpdateHookDelegate Pre;
		public event PostUpdateHookDelegate Post;

		[HarmonyPrefix]
		internal static bool Prefix(Terraria.Main __instance, ref GameTime gameTime) => PHooks.Hook_Main_Update.Pre(__instance, ref gameTime);

		[HarmonyPostfix]
		internal static void Postfix(Terraria.Main __instance, ref GameTime gameTime) => PHooks.Hook_Main_Update.Post(__instance, ref gameTime);
	}
}
