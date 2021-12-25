using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PatchLoader;
using ReLogic.Localization.IME;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.UI.Chat;

namespace InputFix
{
	[HarmonyPatch(typeof(Main), "GetInputText")]
	public class InputFix
	{
		private static bool BackspaceProtection;
		private static FieldInfo _field_backSpaceRate;
		private static FieldInfo Field_backSpaceRate => _field_backSpaceRate ??= PrivateGetter.GetStaticField<Main>("backSpaceRate");

		private static FieldInfo _field_backSpaceCount;
		private static FieldInfo Field_backSpaceCount => _field_backSpaceCount ??= PrivateGetter.GetStaticField<Main>("backSpaceCount");

		private delegate string Delegate_PasteTextIn(bool allowMultiLine, string newKeys);

		private static Delegate_PasteTextIn _method_PasteTextIn;
		private static Delegate_PasteTextIn Method_PasteTextIn => _method_PasteTextIn ??= PrivateGetter.GetStaticMethod<Main>("PasteTextIn").CreateDelegate(typeof(Delegate_PasteTextIn)) as Delegate_PasteTextIn;

		private static float BackSpaceRate
		{
			get => (float)Field_backSpaceRate.GetValue(null);
			set => Field_backSpaceRate.SetValue(null, value);
		}
		private static int BackSpaceCount
		{
			get => (int)Field_backSpaceCount.GetValue(null);
			set => Field_backSpaceCount.SetValue(null, value);
		}

		private static string PasteTextIn(bool allowMultiLine, string newKeys) => Method_PasteTextIn(allowMultiLine, newKeys);

		[HarmonyPrefix]
		public static bool GetInputText_Patched(string oldString, bool allowMultiLine, ref string __result)
		{
			if (!Main.hasFocus)
			{
				__result = oldString;
				return false;
			}
			Main.inputTextEnter = false;
			Main.inputTextEscape = false;
			string text = oldString;
			string text2 = "";
			if (text == null)
			{
				text = "";
			}
			bool flag = false;
			if (Main.inputText.IsKeyDown(Keys.LeftControl) || Main.inputText.IsKeyDown(Keys.RightControl))
			{
				if (Main.inputText.IsKeyDown(Keys.Z) && !Main.oldInputText.IsKeyDown(Keys.Z))
				{
					text = "";
				}
				else if (Main.inputText.IsKeyDown(Keys.X) && !Main.oldInputText.IsKeyDown(Keys.X))
				{
					Platform.Get<IClipboard>().Value = oldString;
					text = "";
				}
				else if ((Main.inputText.IsKeyDown(Keys.C) && !Main.oldInputText.IsKeyDown(Keys.C)) || (Main.inputText.IsKeyDown(Keys.Insert) && !Main.oldInputText.IsKeyDown(Keys.Insert)))
				{
					Platform.Get<IClipboard>().Value = oldString;
				}
				else if (Main.inputText.IsKeyDown(Keys.V) && !Main.oldInputText.IsKeyDown(Keys.V))
				{
					text2 = PasteTextIn(allowMultiLine, text2);
				}
			}
			else
			{
				if (Main.inputText.PressingShift())
				{
					if (Main.inputText.IsKeyDown(Keys.Delete) && !Main.oldInputText.IsKeyDown(Keys.Delete))
					{
						Platform.Get<IClipboard>().Value = oldString;
						text = "";
					}
					if (Main.inputText.IsKeyDown(Keys.Insert) && !Main.oldInputText.IsKeyDown(Keys.Insert))
					{
						text2 = PasteTextIn(allowMultiLine, text2);
					}
				}
				for (int i = 0; i < Main.keyCount; i++)
				{
					int num = Main.keyInt[i];
					string text3 = Main.keyString[i];
					if (num == 13)
					{
						Main.inputTextEnter = true;
					}
					else if (num == 27)
					{
						Main.inputTextEscape = true;
					}
					else if (num >= 32 && num != 127)
					{
						text2 += text3;
					}
				}
			}
			Main.keyCount = 0;
			text += text2;
			Main.oldInputText = Main.inputText;
			Main.inputText = Keyboard.GetState();
			Keys[] pressedKeys = Main.inputText.GetPressedKeys();
			Keys[] pressedKeys2 = Main.oldInputText.GetPressedKeys();
			if (!Main.inputText.IsKeyDown(Keys.Back))
			{
				BackspaceProtection = false;
			}
			if (!string.IsNullOrWhiteSpace(Platform.Get<IImeService>().CompositionString))
			{
				BackspaceProtection = true;
			}
			else if (Main.inputText.IsKeyDown(Keys.Back) && Main.oldInputText.IsKeyDown(Keys.Back) && !BackspaceProtection)
			{
				BackSpaceRate -= 0.05f;
				if (BackSpaceRate < 0f)
				{
					BackSpaceRate = 0f;
				}
				if (BackSpaceCount <= 0)
				{
					BackSpaceCount = (int)Math.Round(BackSpaceRate);
					flag = true;
				}
				BackSpaceCount--;
			}
			else
			{
				BackSpaceRate = 7f;
				BackSpaceCount = 15;
			}
			if (!BackspaceProtection && text.Length > 0)
			{
				for (int j = 0; j < pressedKeys.Length; j++)
				{
					bool flag2 = true;
					for (int k = 0; k < pressedKeys2.Length; k++)
					{
						if (pressedKeys[j] == pressedKeys2[k])
						{
							flag2 = false;
						}
					}
					if ((pressedKeys[j].ToString() ?? "") == "Back" && (flag2 || flag) && text.Length > 0)
					{
						TextSnippet[] array = ChatManager.ParseMessage(text, Color.White).ToArray();
						text = ((!array[array.Length - 1].DeleteWhole) ? text.Substring(0, text.Length - 1) : text.Substring(0, text.Length - array[array.Length - 1].TextOriginal.Length));
					}
				}
			}
			__result = text;
			return false;
		}
	}
}
