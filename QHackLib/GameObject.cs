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
				throw new QHackLibException("Can't get value from ref types");
			return obj.Context.DataTarget.DataReader.Read<T>(obj.InternalObject.Address);
		}
		public unsafe static void SetValue<T>(this GameObject obj, T value) where T : unmanaged
		{
			ClrType type = obj.InternalObject.Type;
			if (type.IsObjectReference)
				throw new QHackLibException("Can't set value to ref types");
			int len = Marshal.SizeOf<T>();
			if (len != type.StaticSize - 8)
				throw new QHackLibException($"Size of object not matched, expected {type.StaticSize}, got {len}");
			IntPtr ptr = Marshal.AllocHGlobal(len);
			Marshal.StructureToPtr(value, ptr, true);
			NativeFunctions.WriteProcessMemory(obj.Context.Handle, obj.BaseAddress, (byte*)ptr, len, 0);
			Marshal.FreeHGlobal(ptr);
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
				throw new QHackLibException("Only array can be indexed");
			if (!indexes.ToList().TrueForAll(t => t is int))
				throw new QHackLibException("Not valid indexes (int only)");
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
				throw new QHackLibException("Only array can be indexed");
			if (!indexes.ToList().TrueForAll(t => t is int))
				throw new QHackLibException("Not valid indexes (int only)");
			Type valueType = value.GetType();
			int[] _indexes = indexes.Select(t => (int)t).ToArray();
			ClrArray array = iobj.AsArray();
			ClrType componentType = array.Type.ComponentType;
			if (value is ClrObject obj)
			{
				if (obj.Type != componentType)
					throw new QHackLibException($"Not the same ref type, target field: {componentType}, value: {obj.Type}");
				int target = (int)obj.Address;
				NativeFunctions.WriteProcessMemory(Context.Handle,
					(int)GameObjectExtension.GetElementAddress(array, 4, _indexes),
					ref target, 4, 0);
			}
			else if (value is ClrValueType val)
			{
				int size = array.Type.ComponentSize;
				if (val.Type.StaticSize != size)
					throw new QHackLibException($"Length not equal, expected {size}, got {val.Type.StaticSize}.");
				byte[] data = new byte[size];
				NativeFunctions.ReadProcessMemory(Context.Handle, (int)val.Address, data, size, 0);
				NativeFunctions.WriteProcessMemory(Context.Handle,
					(int)GameObjectExtension.GetElementAddress(array, size, _indexes),
					data, size, 0);
			}
			else if (valueType.IsValueType)
			{
				int size = Marshal.SizeOf(valueType);
				if (size != array.Type.ComponentSize)
					throw new QHackLibException($"Length not equal, expected {array.Type.ComponentSize}, got {size}.Consider to cast the value's type manually");
				byte[] data = new byte[size];
				IntPtr ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(value, ptr, false);
				Marshal.Copy(ptr, data, 0, size);
				Marshal.FreeHGlobal(ptr);
				NativeFunctions.WriteProcessMemory(Context.Handle,
					(int)GameObjectExtension.GetElementAddress(array, size, _indexes),
					data, size, 0);
			}
			else
			{
				throw new QHackLibException($"Cannot be set to game object: {valueType}");
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
				throw new QHackLibException($"Not the same type, target field: {field.Type}, value: {entity.Type}");
			if (value is ClrObject obj)
			{
				if (obj.Type != field.Type)
					throw new QHackLibException($"Not the same ref type, target field: {field.Type}, value: {obj.Type}");
				int target = (int)obj.Address;
				NativeFunctions.WriteProcessMemory(Context.Handle,
					(int)field.GetAddress(InternalObject.Address),
					ref target, 4, 0);
			}
			else if (value is ClrValueType val)
			{
				int size = val.Type.StaticSize;
				if (size != field.Type.StaticSize)
					throw new QHackLibException($"Length not equal, expected {field.Type.StaticSize}, got {size}.");
				byte[] data = new byte[size];
				NativeFunctions.ReadProcessMemory(Context.Handle, (int)val.Address, data, size, 0);
				NativeFunctions.WriteProcessMemory(Context.Handle,
					(int)field.GetAddress(InternalObject.Address),
					data, size, 0);
			}
			else if (valueType.IsValueType)//except ClrObject/ClrValueType
			{
				int size = Marshal.SizeOf(valueType);
				if (size != field.Type.StaticSize - 8)
					throw new QHackLibException($"Length not equal, expected {field.Type.StaticSize}, got {size}.Consider to cast the value's type manually");
				byte[] data = new byte[size];
				IntPtr ptr = Marshal.AllocHGlobal(size);
				Marshal.StructureToPtr(value, ptr, false);
				Marshal.Copy(ptr, data, 0, size);
				Marshal.FreeHGlobal(ptr);
				NativeFunctions.WriteProcessMemory(Context.Handle,
					(int)field.GetAddress(InternalObject.Address),
					data, size, 0);
			}
			else
			{
				throw new QHackLibException($"Cannot be set to game object: {valueType}");
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
					throw new QHackLibException("Unexpected arg when trying to get a method, only a filter or signature string will be accepcted");
			}
			else
			{
				throw new QHackLibException("More than 1 args when trying to get a method");
			}
			return true;
		}

		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			if (!binder.Type.IsValueType)
				throw new QHackLibException("Cannot convert a game object to ref type");
			int size = Marshal.SizeOf(binder.Type);
			byte[] data = new byte[size];
			NativeFunctions.ReadProcessMemory(Context.Handle, BaseAddress, data, size, 0);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(data, 0, ptr, size);

			result = Marshal.PtrToStructure(ptr, binder.Type);
			Marshal.FreeHGlobal(ptr);
			return true;
		}
	}
}
