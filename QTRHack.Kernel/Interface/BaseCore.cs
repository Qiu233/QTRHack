using QHackLib;
using QTRHack.Kernel.Interface.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface
{
	public abstract class BaseCore
	{
		public GameContext GameContext { get; }
		public CoreVersionSig CoreVersionSig { get; }
		public Version KernelMinimum { get; }

		protected BaseCore(GameContext gameContext)
		{
			GameContext = gameContext;
			CoreVersionSig = CoreVersionSig.Parse(GetType().Assembly.GetCustomAttribute<CoreAttribute>().CoreVersionSig);
			KernelMinimum = Version.Parse(GetType().Assembly.GetCustomAttribute<CoreAttribute>().KernelMinimum);
		}

		/// <summary>
		/// Initialization phase.<br/>
		/// Called when being set to a kernel
		/// </summary>
		public abstract void Initialize();


		public T MakeGameDataAccess<T>(HackObject obj) where T : GameObject
		{
			Type baseType = typeof(T);
			if (!baseType.IsAbstract)
				return baseType.GetConstructor(new Type[] { typeof(BaseCore), typeof(HackObject) }).Invoke(new object[] { this, obj }) as T;
			TypeInfo[] types = GetType().Assembly.DefinedTypes
					   .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract)
					   .ToArray();
			if (types.Length == 0)
				throw new HackKernelException($"Found no implementation of {baseType}");
			else if (types.Length > 1)
				throw new HackKernelException($"Found multiple implementations of {baseType}: {string.Join(", ", types.Select(t => t.FullName))}");
			return types[0].GetConstructor(new Type[] { typeof(BaseCore), typeof(HackObject) }).Invoke(new object[] { this, obj }) as T;
		}


		public static BaseCore GetCore(GameContext ctx, string sig)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(
				t => t.GetCustomAttributes<CoreAttribute>().Count() != 0 &&
				t.GetCustomAttribute<CoreAttribute>().CoreVersionSig == sig).ToArray();
			if (assemblies.Length == 0)
				throw new HackKernelException($"Found no Core of {sig}");
			else if (assemblies.Length > 1)
				throw new HackKernelException($"Found multiple Cores of {sig}: {string.Join(", ", assemblies.Select(t => t.FullName))}");
			Assembly asm = assemblies[0];
			TypeInfo[] ts = asm.DefinedTypes.Where(t => t.IsSubclassOf(typeof(BaseCore))).ToArray();
			if (ts.Length == 0)
				throw new HackKernelException($"Cannot find Core class. Assembly: {asm.FullName}");
			else if (ts.Length > 1)
				throw new HackKernelException($"More than 1 Core class found. Assembly: {asm.FullName}");
			BaseCore core = ts[0].GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
				null, new Type[] { typeof(GameContext) }, null).
				Invoke(new object[] { ctx }) as BaseCore;//construct
			return core;
		}
	}
}
