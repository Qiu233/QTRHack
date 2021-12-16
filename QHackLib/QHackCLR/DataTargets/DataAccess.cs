using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackCLR.DataTargets
{
	/// <summary>
	/// Wrapper for native functions<br/>
	/// Providing fast access to target process<br/>
	/// Not compatible with marshal<br/>
	/// Note, THIS CLASS DOES NOT DISPOSE ANY HANDLE.
	/// </summary>
	public unsafe class DataAccess
	{
		#region Native readers and writers
		[Flags]
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
		[Flags]
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
		internal static extern nuint VirtualAllocEx(
			nuint hProcess,
			nuint lpAddress,
			nuint dwSize,
			AllocationType flAllocationType,
			ProtectionType flProtect);
		[DllImport("kernel32.dll")]
		internal static extern nuint VirtualFreeEx(
			nuint hProcess,
			nuint lpAddress,
			nuint dwSize,
			AllocationType dwFreeType = AllocationType.MEM_RELEASE);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool ReadProcessMemory
		(
			nuint lpProcess,
			nuint lpBaseAddress,
			void* lpBuffer,
			nuint nSize,
			nuint BytesRead
		);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool ReadProcessMemory
		(
			nuint lpProcess,
			nuint lpBaseAddress,
			byte[] lpBuffer,
			nuint nSize,
			nuint BytesRead
		);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool WriteProcessMemory
		(
			nuint lpProcess,
			nuint lpBaseAddress,
			void* lpBuffer,
			nuint nSize,
			nuint BytesWrite
		);
		[DllImport("kernel32.dll")]
		internal unsafe static extern bool WriteProcessMemory
		(
			nuint lpProcess,
			nuint lpBaseAddress,
			byte[] lpBuffer,
			nuint nSize,
			nuint BytesWrite
		);
		#endregion

		#region Wrapped reader and writers

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Read(nuint addr, void* buffer, int length) => ReadProcessMemory(ProcessHandle, addr, buffer, (nuint)length, 0);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Write(nuint addr, void* buffer, int length) => WriteProcessMemory(ProcessHandle, addr, buffer, (nuint)length, 0);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Read<T>(nuint addr, void* pData) where T : unmanaged => Read(addr, pData, sizeof(T));
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Write<T>(nuint addr, void* pValue) where T : unmanaged => Write(addr, pValue, sizeof(T));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Read(nuint addr, in Span<byte> buffer, int length)
		{
			fixed (byte* ptr = buffer)
				return Read(addr, ptr, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Write(nuint addr, in ReadOnlySpan<byte> buffer, int length)
		{
			fixed (byte* ptr = buffer)
				return Write(addr, ptr, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Read<T>(nuint addr, in Span<T> buffer, int length) where T : unmanaged
		{
			fixed (T* ptr = buffer)
				return Read(addr, ptr, length * sizeof(T));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool Write<T>(nuint addr, in ReadOnlySpan<T> buffer, int length) where T : unmanaged
		{
			fixed (T* ptr = buffer)
				return Write(addr, ptr, length * sizeof(T));
		}

		public unsafe bool Read<T>(nuint addr, out T value) where T : unmanaged
		{
			fixed (T* ptr = &value)
				return Read<T>(addr, ptr);
		}
		public unsafe T Read<T>(nuint addr) where T : unmanaged
		{
			Read(addr, out T t);
			return t;
		}
		public unsafe bool Write<T>(nuint addr, in T value) where T : unmanaged
		{
			fixed (T* ptr = &value)
				return Write(addr, ptr, sizeof(T));
		}

		public unsafe bool Read(Type type, nuint addr, out object value)
		{
			if (!type.IsValueType)
				throw new ArgumentException("Not a ValueType", nameof(type));
			int size = Marshal.SizeOf(type);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			bool flag = Read(addr, ptr.ToPointer(), size);
			value = Marshal.PtrToStructure(ptr, type);
			Marshal.FreeHGlobal(ptr);
			return flag;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe object Read(Type type, nuint addr)
		{
			Read(type, addr, out object v);
			return v;
		}

		public unsafe bool Write(nuint addr, object value)
		{
			Type type = value.GetType();
			if (!type.IsValueType)
				throw new ArgumentException("Not a ValueType instance", nameof(value));
			int size = Marshal.SizeOf(type);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(value, ptr, false);
			bool flag = Write(addr, ptr.ToPointer(), size);
			Marshal.FreeHGlobal(ptr);
			return flag;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] ReadBytes(nuint addr, int length)
		{
			byte[] bs = new byte[length];
			Read(addr, bs, length);
			return bs;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteBytes(nuint addr, in ReadOnlySpan<byte> data) => Write(addr, data, data.Length);
		#endregion

		#region Memory allocations
		/// <summary>
		/// Alloc remote memory block
		/// </summary>
		/// <param name="size">The size of memory required</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public nuint AllocMemory(int size = 1024) =>
			AllocMemory(AllocationType.MEM_COMMIT, ProtectionType.PAGE_EXECUTE_READWRITE, size);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal nuint AllocMemory(AllocationType allocationType, ProtectionType protectionType, int size = 1024) =>
			VirtualAllocEx(ProcessHandle, 0, (nuint)size, allocationType, protectionType);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal nuint AllocMemory(nuint addr, AllocationType allocationType, ProtectionType protectionType, int size = 1024) =>
			VirtualAllocEx(ProcessHandle, addr, (nuint)size, allocationType, protectionType);

		/// <summary>
		/// Free a remote memory block
		/// </summary>
		/// <param name="addr"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void FreeMemory(nuint addr) => FreeMemory(addr, AllocationType.MEM_RELEASE);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void FreeMemory(nuint addr, AllocationType allocationType = AllocationType.MEM_RELEASE) => 
			VirtualFreeEx(ProcessHandle, addr, 0, allocationType);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void FreeMemory(nuint addr, nuint size, AllocationType allocationType = AllocationType.MEM_RELEASE) => 
			VirtualFreeEx(ProcessHandle, addr, size, allocationType);
		#endregion

		#region Characters allocations
		/// <summary>
		/// For unicode string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public nuint NewWCHARArray(string str)
		{
			byte[] data = Encoding.Unicode.GetBytes(str);
			nuint addr = AllocMemory(data.Length + 2);
			WriteBytes(addr, data);
			Write<short>(addr + (nuint)data.Length, 0);
			return addr;
		}
		/// <summary>
		/// For ASCII string
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public nuint NewCHARArray(string str)
		{
			byte[] data = Encoding.ASCII.GetBytes(str);
			nuint addr = AllocMemory(data.Length + 1);
			WriteBytes(addr, data);
			Write<byte>(addr + (nuint)data.Length, 0);
			return addr;
		}
		#endregion

		public unsafe static byte[] GetBytes<T>(in T t) where T : unmanaged
		{
			byte[] data = new byte[sizeof(T)];
			fixed (T* ptr = &t)
				for (int i = 0; i < data.Length; i++)
					data[i] = ((byte*)ptr)[i];
			return data;
		}
		public unsafe static T GetValueFromBytes<T>(in ReadOnlySpan<byte> data) where T : unmanaged
		{
			T t = default;
			for (int i = 0; i < data.Length; i++)
				((byte*)&t)[i] = data[i];
			return t;
		}

		public nuint ProcessHandle { get; }
		public DataAccess(nuint handle)
		{
			ProcessHandle = handle;
		}
	}
}
