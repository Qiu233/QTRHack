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
	public sealed class HackMethod : IEquatable<HackMethod>
	{
		public Context Context { get; }
		public ClrMethod InternalClrMethod { get; }

		public HackMethod(Context context, ClrMethod method)
		{
			Context = context;
			InternalClrMethod = method;
		}

		public AssemblyCode Call(bool regProtection, int? thisPtr, int? retBuf, params object[] args)
		{
			return AssemblySnippet.FromClrCall((int)InternalClrMethod.NativeCode, regProtection, thisPtr, retBuf, args);
		}
		public AssemblyCode Call(bool regProtection, IAddressableTypedEntity entity, int? retBuf, params object[] args)
		{
			return Call(regProtection, (int)entity.Address, retBuf, args);
		}
		public HackMethodCall Call(int? thisPtr)
		{
			return new HackMethodCall(this, thisPtr);
		}
		public HackMethodCall Call(IAddressableTypedEntity entity)
		{
			return new HackMethodCall(this, entity);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as HackMethod);
		}
		public override int GetHashCode()
		{
			return InternalClrMethod.GetHashCode();
		}

		public bool Equals(HackMethod other)
		{
			return InternalClrMethod.Equals(other?.InternalClrMethod);
		}

		public static bool operator ==(HackMethod a, HackMethod b)
		{
			if (a == null)
				return b == null;
			return a.Equals(b);
		}
		public static bool operator !=(HackMethod a, HackMethod b)
		{
			return !(a == b);
		}
	}

	/// <summary>
	/// Represents a call to a HackMethod.
	/// </summary>
	public class HackMethodCall
	{
		public HackMethod Method { get; }
		public int? ThisPointer { get; }

		public HackMethodCall(HackMethod method, int? thisPointer)
		{
			Method = method;
			ThisPointer = thisPointer;
		}
		public HackMethodCall(HackMethod method, IAddressableTypedEntity entity)
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
