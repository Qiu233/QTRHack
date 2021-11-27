using Microsoft.Diagnostics.Runtime;
using QHackLib;
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
