﻿using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	public class AobscanHelper
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORY_BASIC_INFORMATION
		{
			public int BaseAddress;
			public int AllocationBase;
			public int AllocationProtect;
			public int RegionSize;
			public int State;
			public DataAccess.ProtectionType Protect;
			public int Type;
		}
		[DllImport("kernel32.dll")]
		private static extern int VirtualQueryEx
		(
			int hProcess,
			int lpAddress,
			out MEMORY_BASIC_INFORMATION lpBuffer,
			int dwLength
		);

		public static string GetMByteCode(int i)
		{
			return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", i & 0xFF, (i >> 8) & 0xFF, (i >> 16) & 0xFF, (i >> 24) & 0xFF);
		}
		private static byte Ctoh(char hex)
		{
			if (hex >= '0' && hex <= '9')
				return (byte)(hex - '0');
			if (hex >= 'A' && hex <= 'F')
				return (byte)(hex - 'A' + 10);
			if (hex >= 'a' && hex <= 'f')
				return (byte)(hex - 'a' + 10);
			return 0;
		}
		public static byte[] GetHexCodeFromString(string str)
		{
			List<byte> bs = new List<byte>();

			char[] a = str.ToCharArray();
			byte t = 0;
			bool flag = false;
			for (int i = 0; i < a.Length; i++)
			{
				if (a[i] != ' ')
				{
					if (flag)
					{
						bs.Add((byte)(t * 0x10 + Ctoh(a[i])));
					}
					t = Ctoh(a[i]);
					flag = !flag;
				}
			}
			return bs.ToArray();
		}
		/// <summary>
		/// 搜索Byte数组
		/// </summary>
		/// <param name="a">源数组</param>
		/// <param name="alen">长度</param>
		/// <param name="b">被搜索的数组</param>
		/// <param name="blen">被搜数组的长度</param>
		/// <returns>失败返回-1</returns>
		public static int Memmem(byte[] a, int alen, byte[] b, int blen)
		{
			int i, j;
			for (i = 0; i < alen - blen; ++i)
			{
				for (j = 0; j < blen; ++j)
					if (a[i + j] != b[j])
						break;
				if (j >= blen)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// 失败返回-1
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="asm"></param>
		/// <returns></returns>
		public static int AobscanASM(int handle, string asm)
		{
			return Aobscan(handle, Assembler.Assemble(asm, 0));
		}
		/// <summary>
		/// 失败返回-1
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="hexCode"></param>
		/// <param name="matching"></param>
		/// <returns></returns>
		public static int Aobscan(int handle, string hexCode, bool matching = false, int block = 0)
		{
			if (!matching)
			{
				byte[] bytes = GetHexCodeFromString(hexCode);
				return Aobscan(handle, bytes, block);
			}
			int i = 0;
			Dictionary<int, byte> pattern = new Dictionary<int, byte>();
			List<int> match = new List<int>();
			foreach (var c in hexCode)
			{
				if (c == ' ') continue;
				else if (c == '*' || i == '?')
				{
					if (!matching)
						throw new Exception("Not in maching mode");
					match.Add(i++);
				}
				else
					pattern[i++] = Convert.ToByte(c.ToString(), 16);
			}
			return AobscanMatch(handle, pattern, match, block);
		}
		private static int AobscanMatch(int handle, Dictionary<int, byte> pattern, List<int> match, int block = 0)
		{
			int i = block;
			while (i < 0x7FFFFFFF)
			{
				int flag = VirtualQueryEx(handle, i, out MEMORY_BASIC_INFORMATION mbi, 28);
				if (flag != 28)
					break;
				if (mbi.RegionSize <= 0)
					break;
				if (mbi.Protect != DataAccess.ProtectionType.PAGE_EXECUTE_READWRITE || mbi.State != 0x00001000)
				{
					i = mbi.BaseAddress + mbi.RegionSize;
					continue;
				}
				byte[] va = new byte[mbi.RegionSize];
				DataAccess.ReadProcessMemory(handle, mbi.BaseAddress, va, mbi.RegionSize, 0);
				int r = MemmemMatch(va, pattern, match);
				if (r >= 0)
				{
					return mbi.BaseAddress + r;
				}
				i = mbi.BaseAddress + mbi.RegionSize;
			}
			return -1;
		}
		private static int MemmemMatch(byte[] v, Dictionary<int, byte> pattern, List<int> match)
		{
			byte GetV(int i)
			{
				int j = v[i / 2];
				if (i % 2 == 0)
					return (byte)(j >> 4);
				else
					return (byte)(j & 0xF);
			}
			int alen = v.Length * 2;
			int blen = pattern.Count + match.Count;

			for (int i = 0; i < alen - blen; i++)
			{
				int j = 0;
				for (; j < blen; j++)
				{
					byte t = GetV(i + j);
					if (match.Contains(j) || t == pattern[j])
						continue;
					break;
				}
				if (j == blen && i % 2 == 0)
					return i / 2;
			}
			return -1;
		}
		/// <summary>
		/// 失败返回-1
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="aob"></param>
		/// <returns></returns>
		public static int Aobscan(int handle, byte[] aob, int blockToStart = 0)
		{
			int i = blockToStart;
			while (i < 0x7FFFFFFF)
			{
				int flag = VirtualQueryEx(handle, i, out MEMORY_BASIC_INFORMATION mbi, 28);
				if (flag != 28)
					break;
				if (mbi.RegionSize <= 0)
					break;
				if (mbi.Protect != DataAccess.ProtectionType.PAGE_EXECUTE_READWRITE || mbi.State != 0x00001000)
				{
					i = mbi.BaseAddress + mbi.RegionSize;
					continue;
				}
				byte[] va = new byte[mbi.RegionSize];
				DataAccess.ReadProcessMemory(handle, mbi.BaseAddress, va, mbi.RegionSize, 0);
				int r = Memmem(va, mbi.RegionSize, aob, aob.Length);
				if (r >= 0)
				{
					return mbi.BaseAddress + r;
				}
				i = mbi.BaseAddress + mbi.RegionSize;
			}
			return -1;
		}
	}
}
