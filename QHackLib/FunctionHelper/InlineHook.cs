using QHackCLR.DataTargets;
using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib.FunctionHelper
{
	public unsafe static class InlineHook
	{
		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct HookInfo
		{
			public const int RAW_CODE_BYTES_LENGTH = 20;
			public static readonly int HeaderSize = sizeof(HookInfo);
			public static readonly int Offset_OnceFlag = (int)Marshal.OffsetOf<HookInfo>(nameof(OnceFlag));
			public static readonly int Offset_SafeFreeFlag = (int)Marshal.OffsetOf<HookInfo>(nameof(SafeFreeFlag));
			public static readonly int Offset_RawCodeLength = (int)Marshal.OffsetOf<HookInfo>(nameof(RawCodeLength));
			public static readonly int Offset_RawCodeBytes = (int)Marshal.OffsetOf<HookInfo>(nameof(RawCodeBytes));

			public nuint Address_Code => AllocationAddress + (uint)HeaderSize;
			public nuint Address_OnceFlag => AllocationAddress + (uint)Offset_OnceFlag;
			public nuint Address_SafeFreeFlag => AllocationAddress + (uint)Offset_SafeFreeFlag;
			public nuint Address_RawCodeLength => AllocationAddress + (uint)Offset_RawCodeLength;
			public nuint Address_RawCodeBytes => AllocationAddress + (uint)Offset_RawCodeBytes;

			public HookInfo(nuint allocationAddress, int onceFlag, int safeFreeFlag, byte[] rawCodeBytes)
			{
				AllocationAddress = allocationAddress;
				OnceFlag = onceFlag;
				SafeFreeFlag = safeFreeFlag;
				RawCodeLength = rawCodeBytes.Length;
				for (int i = 0; i < rawCodeBytes.Length; i++)
					RawCodeBytes[i] = rawCodeBytes[i];
			}

			public nuint AllocationAddress;
			public int OnceFlag;
			public int SafeFreeFlag;
			public int RawCodeLength;
			public fixed byte RawCodeBytes[RAW_CODE_BYTES_LENGTH];
		}

		private static byte[] GetHeadBytes(byte[] code)
		{
			fixed (byte* p = code)
			{
				byte* i = p;
				while (i - p < 5)
					i += Ldasm.GetInst(i, out _, false);
				return code[..(int)(i - p)];
			}
		}

		public static void HookAndWait(Context Context, AssemblyCode code, nuint targetAddr, bool once)
		{
			HookInfo hookInfo = Hook(Context, code, targetAddr, once);
			System.Threading.Thread.Sleep(10);
			nuint sffAddr = hookInfo.Address_SafeFreeFlag;
			nuint ofAddr = hookInfo.Address_OnceFlag;
			while (Context.DataAccess.Read<int>(sffAddr) != 0 ||
					Context.DataAccess.Read<int>(ofAddr) != 0) { }
			Context.DataAccess.Write(targetAddr, hookInfo.RawCodeBytes, hookInfo.RawCodeLength);
			Context.DataAccess.FreeMemory(hookInfo.AllocationAddress);
		}

		public static void FreeHook(Context Context, nuint targetAddr)
		{
			nuint k = targetAddr;

			byte h = Context.DataAccess.Read<byte>(targetAddr);
			if (h != 0xE9) throw new ArgumentException("Not a hooked target");

			int j = Context.DataAccess.Read<int>(targetAddr + 1);
			k += (uint)(j + 5 - HookInfo.HeaderSize);
			HookInfo info = Context.DataAccess.Read<HookInfo>(k);

			Context.DataAccess.Write(targetAddr, info.RawCodeBytes, info.RawCodeLength);
			Context.DataAccess.FreeMemory(info.AllocationAddress);
		}

		private static AssemblyCode GetOnceCheckedCode(AssemblyCode code, nuint onceFlagAddr)
		{
			AssemblySnippet result = AssemblySnippet.FromEmpty();
			result.Content.Add(Instruction.Create("cmp dword ptr [" + onceFlagAddr + "],0"));
			result.Content.Add(Instruction.Create("jle bodyEnd"));
			result.Content.Add(code);
			result.Content.Add(Instruction.Create("dec dword ptr [" + onceFlagAddr + "]"));
			result.Content.Add(Instruction.Create("bodyEnd:"));
			return result;
		}

		/// <summary>
		/// 这个函数被lock了，无法被多个线程同时调用，预防了一些错误
		/// </summary>
		/// <param name="Context"></param>
		/// <param name="code"></param>
		/// <param name="targetAddr"></param>
		/// <param name="once"></param>
		/// <param name="execRaw"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static HookInfo Hook(Context Context, AssemblyCode code, nuint targetAddr, bool once, bool execRaw = true, int size = 1024)
		{
			byte[] headInstBytes = GetHeadBytes(Context.DataAccess.ReadBytes(targetAddr, 32));

			nuint allocAddr = Context.DataAccess.AllocMemory(size);
			nuint safeFreeFlagAddr = allocAddr + (uint)HookInfo.Offset_SafeFreeFlag;
			nuint onceFlagAddr = allocAddr + (uint)HookInfo.Offset_OnceFlag;
			nuint codeAddr = allocAddr + (uint)HookInfo.HeaderSize;
			nuint retAddr = targetAddr + (uint)headInstBytes.Length;

			HookInfo info = new(allocAddr, 1, 1, headInstBytes);

			Assembler assembler = new();
			assembler.Emit(DataAccess.GetBytes(info));//emit the header before runnable code
			assembler.Emit((Instruction)$"mov dword ptr [{safeFreeFlagAddr}],1");
			assembler.Emit(once ? GetOnceCheckedCode(code, onceFlagAddr) : code);//once or not
			if (execRaw)
				assembler.Emit(headInstBytes);//emit the raw code replaced by hook jmp
			assembler.Emit((Instruction)$"mov dword ptr [{safeFreeFlagAddr}],0");
			assembler.Emit((Instruction)$"jmp {retAddr}");


			byte[] jmpToBytesRaw = Assembler.Assemble($"jmp {codeAddr}", targetAddr);
			byte[] jmpToBytes = new byte[headInstBytes.Length];
			for (int i = 0; i < 5; i++)
				jmpToBytes[i] = jmpToBytesRaw[i];
			for (int i = 5; i < headInstBytes.Length; i++)
				jmpToBytes[i] = 0x90;//nop
			Context.DataAccess.WriteBytes(allocAddr, assembler.GetByteCode(allocAddr));
			Context.DataAccess.WriteBytes(targetAddr, jmpToBytes);
			return info;
		}
	}
}
