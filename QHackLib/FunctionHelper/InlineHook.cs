﻿using QHackCLR.DataTargets;
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
			IntPtr ptr3 = Marshal.AllocHGlobal(code.Length);
			Marshal.Copy(code, 0, ptr3, code.Length);
			uint len;
			unsafe
			{
				byte* p = (byte*)ptr3.ToPointer();
				byte* i = p;
				while (i - p < 5)
				{
					Ldasm.ldasm_data data = new Ldasm.ldasm_data();
					uint t = Ldasm.ldasm(i, ref data, false);
					i += t;
				}
				len = (uint)(i - p);
			}
			Marshal.FreeHGlobal(ptr3);
			byte[] v = new byte[len];
			for (int i = 0; i < len; i++)
			{
				v[i] = code[i];
			}
			return v;
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

		/// <summary>
		/// Repoint the jmps
		/// </summary>
		/// <param name="insts"></param>
		/// <param name="rawAddr"></param>
		/// <param name="targetAddr"></param>
		/// <returns></returns>
		private static byte[] ProcessJmps(byte[] insts, nuint rawAddr, nuint targetAddr)
		{
			IntPtr ptr3 = Marshal.AllocHGlobal(insts.Length);
			Marshal.Copy(insts, 0, ptr3, insts.Length);
			unsafe
			{
				byte* p = (byte*)ptr3;
				byte* i = p;
				while (i - p < insts.Length)
				{
					if (*i == 0xe9 || *i == 0xe8)//jmp or call
						*((int*)(i + 1)) += (int)(rawAddr - targetAddr);//move the call
					Ldasm.ldasm_data data = new Ldasm.ldasm_data();
					uint t = Ldasm.ldasm(i, ref data, false);
					i += t;
				}
			}
			byte[] result = new byte[insts.Length];
			Marshal.Copy(ptr3, result, 0, insts.Length);
			Marshal.FreeHGlobal(ptr3);
			return result;
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

			Assembler assembler = new(allocAddr);
			assembler.Emit(DataAccess.GetBytes(info));//emit the header before runnable code
			assembler.Assemble($"mov dword ptr [{safeFreeFlagAddr}],1");
			assembler.Assemble(once ? GetOnceCheckedCode(code, onceFlagAddr) : code);//once or not
			if (execRaw)
				assembler.Emit(ProcessJmps(headInstBytes, targetAddr, assembler.IP));//emit the raw code replaced by hook jmp
			assembler.Assemble($"mov dword ptr [{safeFreeFlagAddr}],0");
			assembler.Assemble("jmp " + retAddr);


			byte[] jmpToBytesRaw = Assembler.Assemble($"jmp {codeAddr}", targetAddr);
			byte[] jmpToBytes = new byte[headInstBytes.Length];
			for (int i = 0; i < 5; i++)
				jmpToBytes[i] = jmpToBytesRaw[i];
			for (int i = 5; i < headInstBytes.Length; i++)
				jmpToBytes[i] = 0x90;//nop

			Context.DataAccess.WriteBytes(allocAddr, assembler.Data.ToArray());
			Context.DataAccess.WriteBytes(targetAddr, jmpToBytes);
			return info;
		}
	}
}
