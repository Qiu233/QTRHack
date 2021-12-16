using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	/// <summary>
	/// <para>Wrapper for native functions</para>
	/// <para>Provide fast access to target process</para>
	/// <para>Not compatible with marshal</para>
	/// </summary>
	public class DataAccess
	{
		#region Native readers and writers
		internal enum ProtectionType : uint
		{
			PAGE_EXECUTE = 0x00000010,
			PAGE_EXECUTE_READ = 0x00000020,
			PAGE_EXECUTE_READWRITE = 0x00000040,
			PAGE_EXECUTE_WRITECOPY = 0x00000080,
			PAGE_NOACCESS = 0x00000001,
			PAGE_READONLY = 0x00000002,
			PAGE_READWRITE = 0x00000004,
			PAGE_WRITECOPY = 0x00000008,
			PAGE_GUARD = 0x00000100,
			PAGE_NOCACHE = 0x00000200,
			PAGE_WRITECOMBINE = 0x00000400
		}
		internal enum AllocationType : uint
		{
			MEM_COMMIT = 0x00001000,
			MEM_RESERVE = 0x00002000,
			MEM_DECOMMIT = 0x00004000,
			MEM_RELEASE = 0x00008000,
			MEM_RESET = 0x00080000,
			MEM_PHYSICAL = 0x00400000,
			MEM_TOPDOWN = 0x00100000,
			MEM_WRITEWATCH = 0x00200000,
			MEM_LARGEPAGES = 0x20000000,
		}

		[DllImport("kernel32.dll")]
		internal static extern IntPtr VirtualAllocEx(
			nuint hProcess,
			IntPtr lpAddress,
			nuint dwSize,
			AllocationType flAllocationType,
			ProtectionType flProtect);
		[DllImport("kernel32.dll")]
		internal static extern IntPtr VirtualFreeEx(
			nuint hProcess,
			IntPtr lpAddress,
			nuint dwSize,
			AllocationType dwFreeType = AllocationType.MEM_RELEASE);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool ReadProcessMemory
		(
			nuint lpProcess,
			IntPtr lpBaseAddress,
			void* lpBuffer,
			nuint nSize,
			nuint BytesRead
		);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool ReadProcessMemory
		(
			nuint lpProcess,
			IntPtr lpBaseAddress,
			byte[] lpBuffer,
			nuint nSize,
			nuint BytesRead
		);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool WriteProcessMemory
		(
			nuint lpProcess,
			IntPtr lpBaseAddress,
			void* lpBuffer,
			nuint nSize,
			nuint BytesWrite
		);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool WriteProcessMemory
		(
			nuint lpProcess,
			IntPtr lpBaseAddress,
			byte[] lpBuffer,
			nuint nSize,
			nuint BytesWrite
		);
		#endregion

		#region Wrapped reader and writers

		public unsafe T Read<T>(IntPtr addr) where T : unmanaged
		{
			T t = default;
			ReadProcessMemory(ProcessHandle, addr, &t, (nuint)sizeof(T), 0);
			return t;
		}
		public unsafe void Write<T>(IntPtr addr, T value) where T : unmanaged
		{
			WriteProcessMemory(ProcessHandle, addr, &value, (nuint)sizeof(T), 0);
		}


		public unsafe void Read<T>(IntPtr addr, void* pData) where T : unmanaged
		{
			ReadProcessMemory(ProcessHandle, addr, pData, (nuint)sizeof(T), 0);
		}
		public unsafe void Write<T>(IntPtr addr, void* pValue) where T : unmanaged
		{
			WriteProcessMemory(ProcessHandle, addr, pValue, (nuint)sizeof(T), 0);
		}

		/// <summary>
		/// Much slower than <see cref="DataAccess.Read&lt;T&rt;"/> because of using marshal
		/// </summary>
		/// <param name="type">ValueType only</param>
		/// <param name="addr"></param>
		/// <returns></returns>
		public unsafe object Read(Type type, IntPtr addr)
		{
			if (!type.IsValueType)
				throw new ArgumentException("Not a ValueType", "type");
			int size = Marshal.SizeOf(type);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			ReadProcessMemory(ProcessHandle, addr, (void*)ptr, (nuint)size, 0);
			object t = Marshal.PtrToStructure(ptr, type);
			Marshal.FreeHGlobal(ptr);
			return t;
		}

		/// <summary>
		/// Much slower than <see cref="DataAccess.Write&lt;T&rt;"/> because of using marshal
		/// </summary>
		/// <param name="addr"></param>
		/// <param name="value">ValueType only</param>
		public unsafe void Write(IntPtr addr, object value)
		{
			Type type = value.GetType();
			if (!type.IsValueType)
				throw new ArgumentException("Not a ValueType instance", "value");
			int size = Marshal.SizeOf(type);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(value, ptr, false);
			WriteProcessMemory(ProcessHandle, addr, (void*)ptr, (nuint)size, 0);
			Marshal.FreeHGlobal(ptr);
		}

		public void Read(IntPtr addr, byte[] buffer, int length)
		{
			if (length > buffer.Length)
				throw new ArgumentException("Length of bytes to read exceeded the buffer", "length");
			ReadProcessMemory(ProcessHandle, addr, buffer, (nuint)length, 0);
		}
		public void Write(IntPtr addr, byte[] buffer, int length)
		{
			if (length > buffer.Length)
				throw new ArgumentException("Length of bytes to write exceeded the buffer", "length");
			WriteProcessMemory(ProcessHandle, addr, buffer, (nuint)length, 0);
		}

		public unsafe void Read(IntPtr addr, void* buffer, int length)
		{
			ReadProcessMemory(ProcessHandle, addr, buffer, (nuint)length, 0);
		}
		public unsafe void Write(IntPtr addr, void* pValue, int length)
		{
			WriteProcessMemory(ProcessHandle, addr, pValue, (nuint)length, 0);
		}

		public byte[] ReadBytes(IntPtr addr, int length)
		{
			byte[] bs = new byte[length];
			ReadProcessMemory(ProcessHandle, addr, bs, (nuint)bs.Length, 0);
			return bs;
		}

		public void WriteBytes(IntPtr addr, byte[] data)
		{
			WriteProcessMemory(ProcessHandle, addr, data, (nuint)data.Length, 0);
		}
		#endregion

		#region Memory allocations
		/// <summary>
		/// Alloc remote memory block
		/// </summary>
		/// <param name="size">The size of memory required</param>
		public IntPtr AllocMemory(int size = 1024)//1 kb
		{
			return AllocMemory(AllocationType.MEM_COMMIT, ProtectionType.PAGE_EXECUTE_READWRITE, size);
		}
		internal IntPtr AllocMemory(AllocationType allocationType, ProtectionType protectionType, int size = 1024)
		{
			return VirtualAllocEx(ProcessHandle, IntPtr.Zero, (nuint)size, allocationType, protectionType);
		}

		/// <summary>
		/// Free a remote memory block
		/// </summary>
		/// <param name="addr"></param>
		public void FreeMemory(IntPtr addr)
		{
			FreeMemory(addr, AllocationType.MEM_RELEASE);
		}
		internal void FreeMemory(IntPtr addr, AllocationType allocationType = AllocationType.MEM_RELEASE)
		{
			VirtualFreeEx(ProcessHandle, IntPtr.Zero, 0, allocationType);
		}
		#endregion

		#region Characters allocations
		/// <summary>
		/// For unicode string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public IntPtr NewWCHARArray(string str)
		{
			byte[] data = Encoding.Unicode.GetBytes(str);
			IntPtr addr = AllocMemory(data.Length + 2);
			WriteBytes(addr, data);
			Write<short>(addr + data.Length, 0);
			return addr;
		}
		/// <summary>
		/// For ASCII string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public IntPtr NewCHARArray(string str)
		{
			byte[] data = Encoding.ASCII.GetBytes(str);
			IntPtr addr = AllocMemory(data.Length + 1);
			WriteBytes(addr, data);
			Write<byte>(addr + data.Length, 0);
			return addr;
		}
		#endregion

		public unsafe static byte[] GetBytes<T>(T t) where T : unmanaged
		{
			byte[] data = new byte[sizeof(T)];
			byte* ptr = (byte*)&t;
			for (int i = 0; i < data.Length; i++)
				data[i] = ptr[i];
			return data;
		}

		public nuint ProcessHandle { get; }
		public int ProcessId { get; }
		public DataAccess(int pid, nuint handle)
		{
			ProcessId = pid;
			ProcessHandle = handle;
		}
	}
}
