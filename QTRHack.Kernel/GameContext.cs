using Microsoft.Diagnostics.Runtime;
using QHackLib;
using QHackLib.Assemble;
using QHackLib.FunctionHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	public class GameContext : IDisposable
	{
		public Context ProcessContext
		{
			get;
		}
		public Process GameProcess
		{
			get;
		}
		public AssemblyName GameAssemblyName
		{
			get;
		}
		public AddressHelper GameAddressHelper
		{
			get;
		}

		private GameContext(Process process)
		{
			GameProcess = process;
			ProcessContext = Context.Create(process.Id);
			GameAddressHelper = ProcessContext.MainAddressHelper;
			GameAssemblyName = AssemblyName.GetAssemblyName(GameProcess.MainModule.FileName);
		}

		public dynamic GetStaticGameObject(string typeName, string fieldName)
		{
			ClrType type = GameAddressHelper.GetClrType(typeName);
			ClrStaticField field = type.GetStaticFieldByName(fieldName);
			return new HackObject(ProcessContext, field.ReadObject(ProcessContext.Runtime.AppDomains[0]));
		}

		public HackMethodCall GetStaticMethod(string typeName, Func<ClrMethod, bool> filter)
		{
			ClrType type = GameAddressHelper.GetClrType(typeName);
			ClrMethod method = type.Methods.First(t => filter(t));
			return new HackMethod(ProcessContext, method).Call(null as int?);
		}
		public HackMethodCall GetStaticMethod(string typeName, string sig)
		{
			return GetStaticMethod(typeName, t => t.Signature == sig);
		}
		public HackMethodCall GetStaticMethodByName(string typeName, string name)
		{
			return GetStaticMethod(typeName, t => t.Name == name);
		}

		/// <summary>
		/// Use DoUpdate hook to create a managed thread.<br/>
		/// </summary>
		/// <remarks>When the thread is finished, remember to dispose the returned RemoteExecution object to release the memory.</remarks>
		/// <param name="codeToRun">void (void)</param>
		/// <returns>An RemoteExecution instance</returns>
		public RemoteThread RunOnManagedThread(AssemblyCode codeToRun)
		{
			int pStr = ProcessContext.DataAccess.NewWCHARArray("System.Action");
			RemoteThread re = RemoteThread.Create(ProcessContext, codeToRun);
			InlineHook.HookAndWait(
				ProcessContext,
				AssemblySnippet.StartManagedThread(
					ProcessContext,
					re.CodeAddress,
					pStr),
				GameAddressHelper.
				GetFunctionAddress("Terraria.Main", "DoUpdate"), true);
			ProcessContext.DataAccess.FreeMemory(pStr);
			return re;
		}


		public static GameContext OpenGame(Process process)
		{
			return new GameContext(process);
		}

		public void Dispose()
		{
			ProcessContext.Dispose();
		}
	}
}
