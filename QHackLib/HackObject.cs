using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Implementation;
using QHackLib;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	public static class HackObjectExtension
	{
		private static readonly MethodInfo Method_GetElementAddress;
		public static Func<ClrArray, int, int[], ulong> GetElementAddress
		{
			get;
		}
		static HackObjectExtension()
		{
			Method_GetElementAddress = typeof(ClrArray).GetMethod("GetElementAddress", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(int[]) }, null);
			GetElementAddress = (array, elementSize, indices) => (ulong)Method_GetElementAddress.Invoke(array, new object[] { elementSize, indices });
		}
		public static T GetValue<T>(this HackObject obj) where T : unmanaged
		{
			ClrType type = obj.InternalClrObject.Type;
			if (type.IsObjectReference)
				throw new HackObject.HackObjectTypeException("Can't get value from ref types.", type.Name);
			return obj.Context.DataAccess.Read<T>(obj.BaseAddress);
		}
		public unsafe static void SetValue<T>(this HackObject obj, T value) where T : unmanaged
		{
			ClrType type = obj.InternalClrObject.Type;
			if (type.IsObjectReference)
				throw new HackObject.HackObjectTypeException("Can't set value to ref types.", type.Name);
			int len = Marshal.SizeOf<T>();
			if (len != type.StaticSize - 8)
				throw new HackObject.HackObjectSizeNotEqualException(type.StaticSize, len);
			obj.Context.DataAccess.Write(obj.BaseAddress, value);
		}
	}
	public class HackObject : DynamicObject, IEquatable<HackObject>
	{
		public Context Context { get; }
		public IAddressableTypedEntity InternalClrObject { get; }

		public int BaseAddress => (int)InternalClrObject.Address;
		public ClrType ClrType => InternalClrObject.Type;

		public HackObject(Context context, IAddressableTypedEntity clrObject)
		{
			Context = context;
			InternalClrObject = clrObject;
		}

		public int GetArrayRank()
		{
			if (!(InternalClrObject is ClrObject iobj) || !iobj.IsArray)
				throw new HackObjectTypeException($"Not an array.", InternalClrObject.Type.Name);
			return iobj.AsArray().Rank;
		}

		public int GetArrayLength()
		{
			if (!(InternalClrObject is ClrObject iobj) || !iobj.IsArray)
				throw new HackObjectTypeException($"Not an array.", InternalClrObject.Type.Name);
			return iobj.AsArray().Length;
		}

		public int GetArrayLength(int i)
		{
			if (!(InternalClrObject is ClrObject iobj) || !iobj.IsArray)
				throw new HackObjectTypeException($"Not an array.", InternalClrObject.Type.Name);
			ClrArray array = iobj.AsArray();
			if (array.Rank < i)
				throw new HackObjectInvalidArgsException($"Not an {i} dimension array.");
			return array.GetLength(i);
		}

		public HackObject InternalGetIndex(object[] indexes)
		{
			if (!(InternalClrObject is ClrObject obj) || !obj.IsArray)
				throw new HackObjectTypeException($"Not an array.", InternalClrObject.Type.Name);
			if (!indexes.ToList().TrueForAll(t => t is int))
				throw new HackObjectInvalidArgsException("Invalid indexes, accepts only int[].");
			int[] _indexes = indexes.Select(t => (int)t).ToArray();
			ClrArray array = obj.AsArray();
			int size = array.Type.ComponentSize;
			if (array.Rank != _indexes.Length)
				throw new HackObjectInvalidArgsException($"Invalid indexes, rank not equal. Expected: {array.Rank}");
			IAddressableTypedEntity v = array.Type.ComponentType.IsObjectReference ? (IAddressableTypedEntity)array.GetObjectValue(_indexes) : array.GetStructValue(_indexes);
			return new HackObject(Context, v);
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			result = InternalGetIndex(indexes);
			return true;
		}

		public void InternalSetIndex(object[] indexes, object value)
		{
			if (!(InternalClrObject is ClrObject iobj) || !iobj.IsArray)
				throw new HackObjectTypeException($"Not an array.", InternalClrObject.Type.Name);
			if (!indexes.ToList().TrueForAll(t => t is int))
				throw new HackObjectInvalidArgsException("Invalid indexes, accepts only int[].");
			Type valueType = value.GetType();
			int[] _indexes = indexes.Select(t => (int)t).ToArray();
			ClrArray array = iobj.AsArray();
			ClrType componentType = array.Type.ComponentType;
			if (array.Rank != _indexes.Length)
				throw new HackObjectInvalidArgsException($"Invalid indexes, rank not equal. Expected: {array.Rank}");
			if (value is ClrObject obj)
			{
				if (obj.Type != componentType)
					throw new HackObjectTypeException($"Not the same ref type as {componentType.Name}.", obj.Type.Name);
				Context.DataAccess.Write((int)HackObjectExtension.GetElementAddress(array, 4, _indexes), (int)obj.Address);
			}
			else if (value is ClrValueType val)
			{
				int size = array.Type.ComponentSize;
				if (val.Type.StaticSize - 8 != size)
					throw new HackObjectSizeNotEqualException(size, val.Type.StaticSize);
				byte[] data = Context.DataAccess.ReadBytes((int)val.Address, size);
				Context.DataAccess.WriteBytes((int)HackObjectExtension.GetElementAddress(array, size, _indexes), data);
			}
			else if (valueType.IsValueType)
			{
				int size = Marshal.SizeOf(valueType);
				if (size != array.Type.ComponentSize)
					throw new HackObjectSizeNotEqualException(array.Type.ComponentSize, size);
				Context.DataAccess.Write((int)HackObjectExtension.GetElementAddress(array, 4, _indexes), value);
			}
			else
			{
				throw new HackObjectTypeException($"Value of ref type cannot be set to a object.", valueType.FullName);
			}
		}

		public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
		{
			InternalSetIndex(indexes, value);
			return true;
		}

		public HackObject InternalGetMember(string name)
		{
			return new HackObject(Context, InternalClrObject.GetFieldFrom(name));
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = InternalGetMember(binder.Name);
			return true;
		}

		public void InternalSetMember(string name, object value)
		{
			Type valueType = value.GetType();
			ClrInstanceField field = InternalClrObject.Type.GetFieldByName(name);
			if (value is IAddressableTypedEntity entity && entity.Type != field.Type)
				throw new HackObjectTypeException($"Not the same type as {field.Type.Name}.", entity.Type.Name);
			if (value is ClrObject obj)
			{
				if (obj.Type != field.Type)
					throw new HackObjectTypeException($"Not the same ref type as {field.Type.Name}.", obj.Type.Name);
				Context.DataAccess.Write((int)field.GetAddress(InternalClrObject.Address), (int)obj.Address);
			}
			else if (value is ClrValueType val)
			{
				int size = val.Type.StaticSize - 8;
				if (size != field.Type.StaticSize - 8)
					throw new HackObjectSizeNotEqualException(field.Type.StaticSize, size);
				byte[] data = Context.DataAccess.ReadBytes((int)val.Address, size);
				Context.DataAccess.WriteBytes((int)field.GetAddress(InternalClrObject.Address), data);
			}
			else if (valueType.IsValueType)//except ClrObject/ClrValueType
			{
				int size = Marshal.SizeOf(valueType);
				if (size != field.Type.StaticSize - 8)
					throw new HackObjectSizeNotEqualException(field.Type.StaticSize, size);
				Context.DataAccess.Write((int)field.GetAddress(InternalClrObject.Address), value);
			}
			else
			{
				throw new HackObjectTypeException($"Value of ref type cannot be set to a object.", valueType.FullName);
			}
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			InternalSetMember(binder.Name, value);
			return true;
		}

		/*public object InternalInvokeMember(string name, object[] args)
		{
			if (args.Length == 0)//default
			{
				return new HackMethod(Context, InternalClrObject.Type.Methods.First(t => t.Name == name)).Call(InternalClrObject);
			}
			else if (args.Length == 1)//filter
			{
				object arg0 = args[0];
				if (arg0 is Func<ClrMethod, bool> filter)
					return new HackMethod(Context, InternalClrObject.Type.Methods.First(t => filter(t))).Call(InternalClrObject);
				else if (arg0 is string sig)
					return new HackMethod(Context, InternalClrObject.Type.Methods.First(t => t.Signature == sig)).Call(InternalClrObject);
				else
					throw new HackObjectInvalidArgsException("Unexpected arg when trying to get a method, accepts only a filter or a signature string.");
			}
			else
			{
				throw new HackObjectInvalidArgsException("More than 1 args when trying to get a method, accepts only a filter or a signature string.");
			}
		}*/

		public HackMethodCall GetMethodCall(string sig)
		{
			return new HackMethod(Context, InternalClrObject.Type.Methods.First(t => t.Signature == sig)).Call(InternalClrObject);
		}
		public HackMethodCall GetMethodCall(Func<ClrMethod, bool> filter)
		{
			return new HackMethod(Context, InternalClrObject.Type.Methods.First(t => filter(t))).Call(InternalClrObject);
		}

		public object InternalConvert(Type type)
		{
			if (!type.IsValueType)
				throw new HackObjectConvertException(type);
			return Context.DataAccess.Read(type, BaseAddress);
		}

		public T InternalConvert<T>() where T : unmanaged
		{
			return Context.DataAccess.Read<T>(BaseAddress);
		}

		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			result = InternalConvert(binder.Type);
			return true;
		}

		public bool Equals(HackObject other)
		{
			return InternalClrObject.Equals(other?.InternalClrObject);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as HackObject);
		}

		public override int GetHashCode()
		{
			return InternalClrObject.GetHashCode();
		}

		public static bool operator ==(HackObject a, HackObject b)
		{
			if (a == null)
				return b == null;
			return a.Equals(b);
		}
		public static bool operator !=(HackObject a, HackObject b)
		{
			return !(a == b);
		}

		internal abstract class HackObjectException : Exception
		{
			public HackObjectException(string msg) : base(msg) { }
		}
		internal class HackObjectTypeException : HackObjectException
		{
			public HackObjectTypeException(string msg, string type) : base($"Type: {type}. {msg}") { }
		}
		internal class HackObjectConvertException : HackObjectException
		{
			public HackObjectConvertException(Type type) : base($"Cannot convert a hack object to ref type: {type}") { }
		}
		internal class HackObjectInvalidArgsException : HackObjectException
		{
			public HackObjectInvalidArgsException(string msg) : base(msg) { }
		}
		internal class HackObjectSizeNotEqualException : HackObjectException
		{
			public HackObjectSizeNotEqualException(int expected, int got) : base($"Size not equal. expected {expected}, however, got {got}.") { }
		}
	}
}
