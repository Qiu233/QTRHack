using QHackLib;
using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using QHackLib.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	public class GameContext : IDisposable
	{
		public QHackContext ProcessContext
		{
			get;
		}
		public Process GameProcess
		{
			get;
		}
		public CLRHelper GameModuleHelper => ProcessContext.CLRHelpers.First(t
			=> string.Equals(Path.GetFullPath(t.Key.GetFileName()),
				Path.GetFullPath(GameProcess.MainModule.FileName),
				StringComparison.OrdinalIgnoreCase))
				.Value;

		private GameContext(Process process)
		{
			GameProcess = process;
			ProcessContext = QHackContext.Create(process.Id);
		}

		public RemoteThread RunOnManagedThread(AssemblyCode codeToRun)
		{
			using MemoryAllocation alloc = new(ProcessContext);
			byte[] bs = Encoding.Unicode.GetBytes("System.Action");
			alloc.Write(bs, (uint)bs.Length, 0);
			alloc.Write<short>(0, (uint)bs.Length);
			RemoteThread re = RemoteThread.Create(ProcessContext, codeToRun);
			InlineHook.HookOnce(
				ProcessContext,
				AssemblySnippet.StartManagedThread(
					ProcessContext,
					re.CodeAddress,
					alloc.AllocationBase),
				GameModuleHelper.
				GetFunctionAddress("Terraria.Main", "DoUpdate")).Wait();
			return re;
		}


		public static GameContext OpenGame(Process process)
		{
			return new GameContext(process);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			ProcessContext.Dispose();
		}

		public void Flush()
		{
			ProcessContext.Flush();
		}

		public async Task<bool> LoadAssembly(string assemblyFile, string typeName)
		{
			using MemoryAllocation alloc = new(ProcessContext);
			var stream = new MemorySpan(ProcessContext, alloc.AllocationBase, (int)alloc.AllocationSize).GetStream();
			nuint pLibAsmStr = stream.IP; stream.WriteWCHARArray(assemblyFile);
			nuint pTypeStr = stream.IP; stream.WriteWCHARArray(typeName);
			nuint loadFrom = ProcessContext.BCLHelper.GetClrMethodBySignature("System.Reflection.Assembly",
				"System.Reflection.Assembly.LoadFrom(System.String)").NativeCode;
			nuint getType = ProcessContext.BCLHelper.GetClrMethodBySignature("System.Reflection.Assembly",
				"System.Reflection.Assembly.GetType(System.String)").NativeCode;
			nuint createInstance = ProcessContext.BCLHelper.GetClrMethodBySignature("System.Activator",
				"System.Activator.CreateInstance(System.Type)").NativeCode;

			var thCode = AssemblySnippet.FromCode(
				new AssemblyCode[] {
					AssemblySnippet.FromConstructString(ProcessContext, pLibAsmStr),
					(Instruction)$"mov ecx,eax",
					(Instruction)$"call {loadFrom}",
					(Instruction)$"push eax",
					AssemblySnippet.FromConstructString(ProcessContext, pTypeStr),
					(Instruction)$"mov edx,eax",
					(Instruction)$"pop ecx",
					(Instruction)$"call {getType}",
					(Instruction)$"mov ecx,eax",
					(Instruction)$"call {createInstance}",
			});
			bool result = await RunOnManagedThread(thCode).WaitToDispose();
			Flush();
			return result;
		}

		public async Task<bool> LoadAssembly(string assemblyFile)
		{
			using MemoryAllocation alloc = new(ProcessContext);
			var stream = new MemorySpan(ProcessContext, alloc.AllocationBase, (int)alloc.AllocationSize).GetStream();
			stream.WriteWCHARArray(assemblyFile);

			nuint loadFrom = ProcessContext.BCLHelper.GetClrMethodBySignature("System.Reflection.Assembly",
				"System.Reflection.Assembly.LoadFrom(System.String)").NativeCode;

			var thCode = AssemblySnippet.FromCode(
				new AssemblyCode[] {
					AssemblySnippet.FromConstructString(ProcessContext, alloc.AllocationBase),
					(Instruction)$"mov ecx,eax",
					(Instruction)$"call {loadFrom}",
			});
			bool result = await RunOnManagedThread(thCode).WaitToDispose();
			Flush();
			return result;
		}
	}
}
