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

		public dynamic MainInstance
		{
			get;
		}

		private GameContext(Process process)
		{
			GameProcess = process;
			ProcessContext = Context.Create(process.Id);
			GameAddressHelper = ProcessContext.MainAddressHelper;
			GameAssemblyName = AssemblyName.GetAssemblyName(GameProcess.MainModule.FileName);
			MainInstance = GetStaticGameObject("Terraria.Main", "instance");
		}

		public dynamic GetStaticGameObject(string typeName, string fieldName)
		{
			ClrType type = GameAddressHelper.GetClrType(typeName);
			ClrStaticField field = type.GetStaticFieldByName(fieldName);
			return new GameObject(ProcessContext, field.ReadObject(ProcessContext.Runtime.AppDomains[0]));
		}

		public GameMethodCall GetStaticMethod(string typeName, Func<ClrMethod, bool> filter)
		{
			ClrType type = GameAddressHelper.GetClrType(typeName);
			ClrMethod method = type.Methods.First(t => filter(t));
			return new GameMethod(ProcessContext, method).Call(null as int?);
		}
		public GameMethodCall GetStaticMethod(string typeName, string sig)
		{
			return GetStaticMethod(typeName, t => t.Signature == sig);
		}
		public GameMethodCall GetStaticMethodByName(string typeName, string name)
		{
			return GetStaticMethod(typeName, t => t.Name == name);
		}

		/// <summary>
		/// To hook DoUpdate and wait until the code run once.<br/>
		/// This method is thread-safe.
		/// </summary>
		/// <param name="code"></param>
		public void HookDoUpdate(AssemblyCode code)
		{
			lock (__DoUpdateLock)
			{
				int dou = ProcessContext.MainAddressHelper.GetFunctionAddress("Terraria.Main", "DoUpdate");
				InlineHook.HookAndWait(ProcessContext, code, dou, true);
			}
		}
		private static readonly object __DoUpdateLock = new object();

		/// <summary>
		/// Use DoUpdate hook to create a managed thread.<br/>
		/// </summary>
		/// <remarks>When the thread is finished, remember to dispose the returned RemoteExecution object to release the memory.</remarks>
		/// <param name="codeToRun">void (void)</param>
		/// <returns>An RemoteExecution instance</returns>
		public RemoteExecution RunOnManagedThread(AssemblyCode codeToRun)
		{
			int pStr = ProcessContext.DataAccess.NewWCHARArray("System.Action");
			RemoteExecution re = RemoteExecution.Create(ProcessContext, codeToRun);
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
