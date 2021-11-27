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
	public static class GameObjectExtension
	{
		private static MethodInfo Method_GetElementAddress
		{
			get;
		}
		public static Func<ClrArray, int, int[], ulong> GetElementAddress
		{
			get;
		}
		static GameObjectExtension()
		{
			Method_GetElementAddress = typeof(ClrArray).GetMethod("GetElementAddress", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(int[]) }, null);
			GetElementAddress = (array, elementSize, indices) => (ulong)Method_GetElementAddress.Invoke(array, new object[] { elementSize, indices });

		}
		public static T GetValue<T>(this GameObject obj) where T : unmanaged
		{
			ClrType type = obj.InternalObject.Type;
			if (type.IsObjectReference)
				throw new GameObject.GameObjectTypeException("Can't get value from ref types.", type.Name);
			return obj.Context.DataAccess.Read<T>(obj.BaseAddress);
		}
		public unsafe static void SetValue<T>(this GameObject obj, T value) where T : unmanaged
		{
			ClrType type = obj.InternalObject.Type;
			if (type.IsObjectReference)
				throw new GameObject.GameObjectTypeException("Can't set value to ref types.", type.Name);
			int len = Marshal.SizeOf<T>();
			if (len != type.StaticSize - 8)
				throw new GameObject.GameObjectSizeNotEqualException(type.StaticSize, len);
			obj.Context.DataAccess.Write(obj.BaseAddress, value);
		}
	}
	public class GameObject : DynamicObject
	{
		public Context Context { get; }
		public IAddressableTypedEntity InternalObject { get; }

		public int BaseAddress => (int)InternalObject.Address;
		public ClrType ClrType => InternalObject.Type;

		public GameObject(Context context, IAddressableTypedEntity clrObject)
		{
			Context = context;
			InternalObject = clrObject;
		}

		public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
		{
			if (!(InternalObject is ClrObject obj) || !obj.IsArray)
				throw new GameObjectTypeException($"Not an indexable type.", InternalObject.Type.Name);
			if (!indexes.ToList().TrueForAll(t => t is int))
				throw new GameObjectInvalidArgsException("Not valid indexes, accepts only int[].");
			int[] _indexes = indexes.Select(t => (int)t).ToArray();
			ClrArray array = obj.AsArray();
			int size = array.Type.ComponentSize;
			IAddressableTypedEntity v = array.Type.ComponentType.IsObjectReference ? (IAddressableTypedEntity)array.GetObjectValue(_indexes) : array.GetStructValue(_indexes);
			result = new GameObject(Context, v);
			return true;
		}

		public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
		{
			if (!(InternalObject is ClrObject iobj) || !iobj.IsArray)
				throw new GameObjectTypeException($"Not an indexable type.", InternalObject.Type.Name);
			if (!indexes.ToList().TrueForAll(t => t is int))
				throw new GameObjectInvalidArgsException("Not valid indexes, accepts only int[].");
			Type valueType = value.GetType();
			int[] _indexes = indexes.Select(t => (int)t).ToArray();
			ClrArray array = iobj.AsArray();
			ClrType componentType = array.Type.ComponentType;
			if (value is ClrObject obj)
			{
				if (obj.Type != componentType)
					throw new GameObjectTypeException($"Not the same ref type as {componentType.Name}.", obj.Type.Name);
				Context.DataAccess.Write((int)GameObjectExtension.GetElementAddress(array, 4, _indexes), (int)obj.Address);
			}
			else if (value is ClrValueType val)
			{
				int size = array.Type.ComponentSize;
				if (val.Type.StaticSize - 8 != size)
					throw new GameObjectSizeNotEqualException(size, val.Type.StaticSize);
				byte[] data = Context.DataAccess.ReadBytes((int)val.Address, size);
				Context.DataAccess.WriteBytes((int)GameObjectExtension.GetElementAddress(array, size, _indexes), data);
			}
			else if (valueType.IsValueType)
			{
				int size = Marshal.SizeOf(valueType);
				if (size != array.Type.ComponentSize)
					throw new GameObjectSizeNotEqualException(array.Type.ComponentSize, size);
				Context.DataAccess.Write((int)GameObjectExtension.GetElementAddress(array, 4, _indexes), value);
			}
			else
			{
				throw new GameObjectTypeException($"Value of this type cannot be set to a game object.", valueType.FullName);
			}
			return true;
		}


		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = new GameObject(Context, InternalObject.GetFieldFrom(binder.Name));
			return true;
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			Type valueType = value.GetType();
			ClrInstanceField field = InternalObject.Type.GetFieldByName(binder.Name);
			if (value is IAddressableTypedEntity entity && entity.Type != field.Type)
				throw new GameObjectTypeException($"Not the same type as {field.Type.Name}.", entity.Type.Name);
			if (value is ClrObject obj)
			{
				if (obj.Type != field.Type)
					throw new GameObjectTypeException($"Not the same ref type as {field.Type.Name}.", obj.Type.Name);
				Context.DataAccess.Write((int)field.GetAddress(InternalObject.Address), (int)obj.Address);
			}
			else if (value is ClrValueType val)
			{
				int size = val.Type.StaticSize - 8;
				if (size != field.Type.StaticSize - 8)
					throw new GameObjectSizeNotEqualException(field.Type.StaticSize, size);
				byte[] data = Context.DataAccess.ReadBytes((int)val.Address, size);
				Context.DataAccess.WriteBytes((int)field.GetAddress(InternalObject.Address), data);
			}
			else if (valueType.IsValueType)//except ClrObject/ClrValueType
			{
				int size = Marshal.SizeOf(valueType);
				if (size != field.Type.StaticSize - 8)
					throw new GameObjectSizeNotEqualException(field.Type.StaticSize, size);
				Context.DataAccess.Write((int)field.GetAddress(InternalObject.Address), value);
			}
			else
			{
				throw new GameObjectTypeException($"Value of this type cannot be set to a game object.", valueType.FullName);
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="binder"></param>
		/// <param name="args"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
		{
			if (args.Length == 0)//default
			{
				result = new GameMethod(Context, InternalObject.Type.Methods.First(t => t.Name == binder.Name)).Call(InternalObject);
			}
			else if (args.Length == 1)//filter
			{
				object arg0 = args[0];
				if (arg0 is Func<ClrMethod, bool> filter)
					result = new GameMethod(Context, InternalObject.Type.Methods.First(t => filter(t))).Call(InternalObject);
				else if (arg0 is string sig)
					result = new GameMethod(Context, InternalObject.Type.Methods.First(t => t.Signature == sig)).Call(InternalObject);
				else
					throw new GameObjectInvalidArgsException("Unexpected arg when trying to get a method, accepts only a filter or a signature string.");
			}
			else
			{
				throw new GameObjectInvalidArgsException("More than 1 args when trying to get a method, accepts only a filter or a signature string.");
			}
			return true;
		}

		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			if (!binder.Type.IsValueType)
				throw new GameObjectConvertException(binder.Type);
			result = Context.DataAccess.Read(binder.Type, BaseAddress);
			return true;
		}

		internal abstract class GameObjectException : Exception
		{
			public GameObjectException(string msg) : base(msg) { }
		}
		internal class GameObjectTypeException : GameObjectException
		{
			public GameObjectTypeException(string msg, string type) : base($"Type: {type}. {msg}") { }
		}
		internal class GameObjectConvertException : GameObjectException
		{
			public GameObjectConvertException(Type type) : base($"Cannot convert a game object to ref type: {type}") { }
		}
		internal class GameObjectInvalidArgsException : GameObjectException
		{
			public GameObjectInvalidArgsException(string msg) : base(msg) { }
		}
		internal class GameObjectSizeNotEqualException : GameObjectException
		{
			public GameObjectSizeNotEqualException(int expected, int got) : base($"Size not equal. expected {expected}, however, got {got}.") { }
		}
	}
}
