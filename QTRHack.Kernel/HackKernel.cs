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
		public BaseCore Core
		{
			get;
			private set;
		}

		private HackKernel(Process process)
		{
			GameContext = GameContext.OpenGame(process);
		}

		public void SetCore(BaseCore core)
		{
			core.Initialize(GameContext);
			Core = core;
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
			var type = Core.GameDataProvider.GetType();
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
			return handlers[0].GetValue(Core.GameDataProvider) as GDAccess<T>;
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
