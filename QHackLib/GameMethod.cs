using Microsoft.Diagnostics.Runtime;
using QHackLib.Assemble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	/// <summary>
	/// A wrapper for game method
	/// </summary>
	public class GameMethod : IEquatable<GameMethod>
	{
		public Context Context { get; }
		public ClrMethod Method { get; }

		public GameMethod(Context context, ClrMethod method)
		{
			Context = context;
			Method = method;
		}

		public AssemblyCode Call(bool regProtection, int? thisPtr, int? retBuf, params object[] args)
		{
			return AssemblySnippet.FromClrCall((int)Method.NativeCode, regProtection, thisPtr, retBuf, args);
		}
		public AssemblyCode Call(bool regProtection, IAddressableTypedEntity entity, int? retBuf, params object[] args)
		{
			return Call(regProtection, (int)entity.Address, retBuf, args);
		}
		public GameMethodCall Call(int? thisPtr)
		{
			return new GameMethodCall(this, thisPtr);
		}
		public GameMethodCall Call(IAddressableTypedEntity entity)
		{
			return new GameMethodCall(this, entity);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as GameMethod);
		}
		public override int GetHashCode()
		{
			return Method.GetHashCode();
		}

		public bool Equals(GameMethod other)
		{
			return Method.Equals(other?.Method);
		}

		public static bool operator ==(GameMethod a, GameMethod b)
		{
			if (a == null)
				return b == null;
			return a.Equals(b);
		}
		public static bool operator !=(GameMethod a, GameMethod b)
		{
			return !(a == b);
		}
	}
	public class GameMethodCall
	{
		public GameMethod Method { get; }
		public int? ThisPointer { get; }

		public GameMethodCall(GameMethod method, int? thisPointer)
		{
			Method = method;
			ThisPointer = thisPointer;
		}
		public GameMethodCall(GameMethod method, IAddressableTypedEntity entity)
		{
			Method = method;
			ThisPointer = (int)entity.Address;
		}
		public AssemblyCode Call(bool regProtection, int? retBuf, params object[] args)
		{
			return Method.Call(regProtection, ThisPointer, retBuf, args);
		}
	}
}
