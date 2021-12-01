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
			core.Initialize();
			Core = core;
		}

		public T MakeGameObject<T>(HackObject obj) where T : GameObject
		{
			return Core.MakeGameDataAccess<T>(obj);
		}

		public T GetStaticGameObject<T>(string typeName, string fieldName) where T : GameObject
		{
			return MakeGameObject<T>(GameContext.GetStaticGameObject(typeName, fieldName));
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
	}
}
