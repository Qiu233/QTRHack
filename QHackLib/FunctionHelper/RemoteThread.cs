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
	public unsafe sealed class RemoteThread : IDisposable
	{
		[DllImport("kernel32.dll")]
		internal static extern IntPtr CreateRemoteThread(
			nuint hProcess,
			nuint lpThreadAttributes,
			int dwStackSize,
			nuint lpStartAddress, // in remote process
			nuint lpParameter,
			uint dwCreationFlags,
			out int lpThreadId
		);
		private readonly RemoteThreadHeader Header;

		/// <summary>
		/// Indicates whether the code memory can be safely released.<br/>
		/// Note that this 
		/// </summary>
		public nuint SafeFreeFlagAddress => Header.Address_SafeFreeFlag;
		public nuint CodeAddress => Header.Address_Code;

		public Context Context
		{
			get;
		}
		public int ThreadID
		{
			get;
			private set;
		}
		private RemoteThread(Context ctx, AssemblyCode asm)
		{
			Context = ctx;
			Header = new RemoteThreadHeader
			{
				AllocationAddress = Context.DataAccess.AllocMemory(),
				SafeFreeFlag = 1
			};

			Assembler assembler = new(Header.AllocationAddress);
			assembler.Emit(DataAccess.GetBytes(Header));
			assembler.Assemble($"mov dword ptr [{Header.Address_SafeFreeFlag}],1");
			assembler.Assemble(asm);
			assembler.Assemble($"mov dword ptr [{Header.Address_SafeFreeFlag}],0");
			assembler.Assemble("ret");
			Context.DataAccess.WriteBytes(Header.AllocationAddress, assembler.Data.ToArray());
		}

		/// <summary>
		/// Allocates space and fills the code in before calling <see cref="RunOnNativeThread"/> to start a remote native thread.<br/>
		/// To avoid a memory leak, call <see cref="Dispose"/> to release the allocated space when the thread is not running.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="asm"></param>
		/// <returns></returns>
		public static RemoteThread Create(Context ctx, AssemblyCode asm) => new RemoteThread(ctx, asm);

		/// <summary>
		/// Directly starts a remote native thread.<br/>
		/// Note that native threads have no clr info and hence cannot do such things like allocating space on clr heaps.
		/// </summary>
		/// <returns>ThreadID of the remote thread created</returns>
		public int RunOnNativeThread()
		{
			CreateRemoteThread(Context.Handle, 0, 0, Header.Address_Code, 0, 0, out int tid);
			ThreadID = tid;
			return ThreadID;
		}

		/// <summary>
		/// Starts a managed thread.<br/>
		/// Pending implementation.
		/// </summary>
		public void RunOnManagedThread()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Calling this method will wait until the SafeFreeFlag become 0 before releasing the memory,<br/>
		/// which ensures that the code will be executed at least once.
		/// </summary>
		public void Dispose()
		{
			nuint sffAddr = Header.Address_SafeFreeFlag;
			while (Context.DataAccess.Read<int>(sffAddr) != 0) { }
			Context.DataAccess.FreeMemory(Header.AllocationAddress);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RemoteThreadHeader
		{
			public static readonly int HeaderSize = sizeof(RemoteThreadHeader);
			public static readonly int Offset_SafeFreeFlag = (int)Marshal.OffsetOf<RemoteThreadHeader>(nameof(SafeFreeFlag));
			public nuint Address_Code => AllocationAddress + (nuint)HeaderSize;
			public nuint Address_SafeFreeFlag => AllocationAddress + (nuint)Offset_SafeFreeFlag;

			public nuint AllocationAddress;
			public int SafeFreeFlag;
		}
	}
}
