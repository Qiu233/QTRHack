using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib.FunctionHelper
{
	public unsafe class InlineHook
	{
		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct HookInfo
		{
			public const int RawCodeBytesLength = 20;
			public static readonly int HeaderSize = sizeof(HookInfo);
			public static readonly int Offset_OnceFlag = (int)Marshal.OffsetOf<HookInfo>("OnceFlag");
			public static readonly int Offset_SafeFreeFlag = (int)Marshal.OffsetOf<HookInfo>("SafeFreeFlag");
			public static readonly int Offset_RawCodeLength = (int)Marshal.OffsetOf<HookInfo>("RawCodeLength");
			public static readonly int Offset_RawCodeBytes = (int)Marshal.OffsetOf<HookInfo>("RawCodeBytes");

			public int Address_Code => AllocationAddress + HeaderSize;
			public int Address_OnceFlag => AllocationAddress + Offset_OnceFlag;
			public int Address_SafeFreeFlag => AllocationAddress + Offset_SafeFreeFlag;
			public int Address_RawCodeLength => AllocationAddress + Offset_RawCodeLength;
			public int Address_RawCodeBytes => AllocationAddress + Offset_RawCodeBytes;

			public HookInfo(int allocationAddress, int onceFlag, int safeFreeFlag, byte[] rawCodeBytes)
			{
				AllocationAddress = allocationAddress;
				OnceFlag = onceFlag;
				SafeFreeFlag = safeFreeFlag;
				RawCodeLength = rawCodeBytes.Length;
				for (int i = 0; i < rawCodeBytes.Length; i++)
					RawCodeBytes[i] = rawCodeBytes[i];
			}

			public int AllocationAddress;
			public int OnceFlag;
			public int SafeFreeFlag;
			public int RawCodeLength;
			public fixed byte RawCodeBytes[RawCodeBytesLength];
		}
		private InlineHook() { }

		public static byte[] GetHeadBytes(byte[] code)
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

		public static void InjectAndWait(Context Context, AssemblyCode snippet, int targetAddr, bool once)
		{
			HookInfo hookInfo = Inject(Context, snippet, targetAddr, once);
			System.Threading.Thread.Sleep(10);
			int sffAddr = hookInfo.Address_SafeFreeFlag;
			int ofAddr = hookInfo.Address_OnceFlag;
			while (Context.DataAccess.Read<int>(sffAddr) != 0 ||
					Context.DataAccess.Read<int>(ofAddr) != 0) { }
			Context.DataAccess.Write(targetAddr, hookInfo.RawCodeBytes, hookInfo.RawCodeLength);
			Context.DataAccess.FreeMemory(hookInfo.AllocationAddress);
		}

		public static void FreeHook(Context Context, int targetAddr)
		{
			int k = targetAddr;

			byte h = Context.DataAccess.Read<byte>(targetAddr);
			if (h != 0xE9) throw new ArgumentException("Not a hooked target");

			int j = Context.DataAccess.Read<int>(targetAddr + 1);
			k += j + 5 - HookInfo.HeaderSize;
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
		private static byte[] ProcessJmps(byte[] insts, int rawAddr, int targetAddr)
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
						*((int*)(i + 1)) += rawAddr - targetAddr;//move the call
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

		private static AssemblyCode GetOnceCheckedCode(AssemblyCode code, int onceFlagAddr)
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
		public static HookInfo Inject(Context Context, AssemblyCode code, int targetAddr, bool once, bool execRaw = true, int size = 1024)
		{
			byte[] headInstBytes = GetHeadBytes(Context.DataAccess.ReadBytes(targetAddr, 32));

			int allocAddr = Context.DataAccess.AllocMemory(size);
			int safeFreeFlagAddr = allocAddr + HookInfo.Offset_SafeFreeFlag;
			int onceFlagAddr = allocAddr + HookInfo.Offset_OnceFlag;
			int codeAddr = allocAddr + HookInfo.HeaderSize;
			int retAddr = targetAddr + headInstBytes.Length;

			HookInfo info = new HookInfo(allocAddr, 1, 1, headInstBytes);

			Assembler assembler = new Assembler(allocAddr);
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
