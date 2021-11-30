using Microsoft.Diagnostics.Runtime;
using QHackLib.FunctionHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	public class Context : IDisposable
	{
		#region natives
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct LUID
		{
			public int LowPart;
			public uint HighPart;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;
			public uint Attributes;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct TOKEN_PRIVILEGES
		{
			public int PrivilegeCount;
			public LUID_AND_ATTRIBUTES Privilege;
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetCurrentProcess();

		[DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccesss, out IntPtr TokenHandle);


		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, [MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

		[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, [MarshalAs(UnmanagedType.Struct)] ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, uint ReturnLength);

		[DllImport("kernel32.dll")]
		private static extern int OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);
		[DllImport("kernel32.dll")]
		private static extern int CloseHandle(int dwDesiredAccess);
		private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
		private const uint TOKEN_QUERY = 0x0008;
		private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
		private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
		#endregion

		private static void GrantPrivilege()
		{
			LUID locallyUniqueIdentifier = new LUID();
			LookupPrivilegeValue(null, "SeDebugPrivilege", ref locallyUniqueIdentifier);
			TOKEN_PRIVILEGES tokenPrivileges = new TOKEN_PRIVILEGES
			{
				PrivilegeCount = 1
			};

			LUID_AND_ATTRIBUTES luidAndAtt = new LUID_AND_ATTRIBUTES
			{
				Attributes = SE_PRIVILEGE_ENABLED,
				Luid = locallyUniqueIdentifier
			};
			tokenPrivileges.Privilege = luidAndAtt;

			OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle);
			AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 1024, IntPtr.Zero, 0);
			CloseHandle(tokenHandle);
		}

		public string ProcessName { get; }
		public int ProcessID { get; }
		public int Handle { get; }

		public DataTarget DataTarget { get; }
		public ClrRuntime Runtime { get; }
		public AddressHelper MainAddressHelper { get; }
		public AddressHelper MSCORLIBAddressHelper { get; }
		public AddressHelper[] AddressHelpers { get; }
		private Dictionary<string, AddressHelper> NameToAddressHelper { get; }
		public DataAccess DataAccess => new DataAccess(Handle);


		private void LoadAllAddressHelpers()
		{
			for (int i = 0; i < Runtime.AppDomains[0].Modules.Length; i++)
				AddressHelpers[i] = CreateFunctionAddressHelper(Runtime.AppDomains[0].Modules[i]);
		}

		private Context(string name, int id, int handle, string moduleName)
		{
			GrantPrivilege();
			ProcessName = name;
			ProcessID = id;
			Handle = handle;
			DataTarget = DataTarget.AttachToProcess(id, false);
			Runtime = DataTarget.ClrVersions[0].CreateRuntime();

			AddressHelpers = new AddressHelper[Runtime.AppDomains[0].Modules.Length];
			NameToAddressHelper = new Dictionary<string, AddressHelper>();
			LoadAllAddressHelpers();

			MainAddressHelper = GetAddressHelper(moduleName);//这句必须最后执行，因为需要用到Context里面的一点信息
			MSCORLIBAddressHelper = GetAddressHelper("mscorlib.dll");
		}

		public AddressHelper GetAddressHelper(string ModuleName)
		{
			if (NameToAddressHelper.ContainsKey(ModuleName))
				return NameToAddressHelper[ModuleName];
			var m = AddressHelpers.First(t =>
			{
				if (t == null || t.Module == null || t.Module.Name == null)
					return false;
				return t.Module.Name.Contains("\\") ? Path.GetFileName(t.Module.Name) == ModuleName :
				t.Module.Name != "dynamic" && t.Module.Name.Substring(0, t.Module.Name.IndexOf(",")) == Path.GetFileNameWithoutExtension(ModuleName);
			});
			NameToAddressHelper[ModuleName] = m;
			return m;
		}

		private AddressHelper CreateFunctionAddressHelper(ClrModule module)
		{
			return new AddressHelper(this, module);
		}

		public static Context Create(int processID)
		{
			Process process = Process.GetProcessById(processID);
			return new Context(process.ProcessName, processID, OpenProcess(PROCESS_ALL_ACCESS, false, processID), process.ProcessName + ".exe");
		}

		public void Dispose()
		{
			DataTarget.Dispose();
		}
	}
}
