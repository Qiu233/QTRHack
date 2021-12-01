using QHackLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	public abstract class GameObject : IEquatable<GameObject>
	{
		public BaseCore Core { get; }
		public dynamic InternalObject { get; }
		public HackObject TypedInternalObject => InternalObject as HackObject;
		protected GameObject(BaseCore core, HackObject obj)
		{
			Core = core;
			InternalObject = obj;
		}

		public bool Equals(GameObject other)
		{
			return InternalObject.Equals(other?.InternalObject);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as GameObject);
		}

		public override int GetHashCode()
		{
			return InternalObject.GetHashCode();
		}

		public static bool operator ==(GameObject a, GameObject b)
		{
			if (a == null)
				return b == null;
			return a.Equals(b);
		}
		public static bool operator !=(GameObject a, GameObject b)
		{
			return !(a == b);
		}
	}
}
