using Microsoft.Diagnostics.Runtime;
using QHackLib;
using QTRHack.Kernel.Interface;
using QTRHack.Kernel.Interface.GameData;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	/// <summary>
	/// One kernel to one game instance
	/// </summary>
	public class HackKernel : IDisposable
	{
		public GameContext GameContext { get; }
		private readonly List<BaseCore> _Cores;
		private BaseCore _WorkingCore;
		private const string DIR_CORE = "./Cores/";

		private HackKernel(Process process)
		{
			GameContext = GameContext.OpenGame(process);
			_Cores = new List<BaseCore>();
			LoadCores(DIR_CORE);
		}

		private void LoadCore(string file)
		{
			Assembly asm = Assembly.LoadFrom(file);
			TypeInfo[] ts = asm.DefinedTypes.Where(t => t.IsSubclassOf(typeof(BaseCore))).ToArray();
			if (ts.Length == 0)
				throw new HackKernelException($"Cannot find Core class. File: {Path.GetFullPath(file)}");
			else if (ts.Length > 1)
				throw new HackKernelException($"More than 1 Core class found. File: {Path.GetFullPath(file)}");
			BaseCore core = ts[0].GetConstructor(new Type[] { typeof(GameContext) }).
				Invoke(new object[] { GameContext }) as BaseCore;//construct
			_Cores.Add(core);
		}

		/// <summary>
		/// Load all files as Core
		/// </summary>
		/// <param name="dir"></param>
		private void LoadCores(string dir)
		{
			foreach (string file in Directory.EnumerateFiles(dir, "*.dll"))
				LoadCore(file);
			foreach (var core in _Cores)
			{
				if (core.MatchGame())
				{
					core.Initialize();
					_WorkingCore = core;//select this
					break;
				}
			}
		}

		/// <summary>
		/// To attach to a game instance
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public static HackKernel Create(Process process)
		{
			return new HackKernel(process);
		}

		public void Dispose()
		{
			GameContext.Dispose();
		}

		private GDAccess<T> GetGDHandler<T>() where T : GDAccessArgs
		{
			var core = _WorkingCore;
			var type = core.GameDataProvider.GetType();
			Type vType = typeof(T);
			PropertyInfo[] handlers = type.
				GetProperties().Where(
					t => t.PropertyType.IsSubclassOf(typeof(GDAccess<T>))).ToArray();//find all possible handlers
			if (vType == typeof(GDAccessArgs))
				throw new HackKernelException($"Cannot get handler for {typeof(GDAccessArgs).FullName}");
			if (handlers.Length > 1)//more than 1 found
			{
				throw new HackKernelException($"More than 1 request handler that accepts {vType.FullName} found in class: {type.FullName}");
			}
			else if (handlers.Length == 0)//no handler found
			{
				throw new HackKernelException($"Cannot get handler for {vType.FullName}");
			}
			return handlers[0].GetValue(core.GameDataProvider) as GDAccess<T>;
		}

		public V RequestGD<T, V>(GDRequest<T> request) where T : GDAccessArgs
		{
			return GetGDHandler<T>().Request<V>(request);
		}

		public V RequestGD<T, V>(string mode, T args) where T : GDAccessArgs
		{
			return RequestGD<T, V>(new GDRequest<T>() { Args = args, Mode = mode });
		}
	}
}
