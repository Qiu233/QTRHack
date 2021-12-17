using QHackCLR.Clr;
using QHackLib.QHackCLR.Clr.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	public unsafe class AddressHelper
	{
		public Context Context { get; }
		public ClrModule Module { get; }
		public string ModuleName { get => Module.Name; }
		public nuint this[string TypeName, string FunctionName]
		{
			get => GetFunctionAddress(TypeName, FunctionName);
		}
		/*public ILToNativeMap this[string TypeName, string FunctionName, int ILOffset]
		{
			get => GetFunctionInstruction(TypeName, FunctionName, ILOffset);
		}*/
		internal AddressHelper(Context ctx, ClrModule module)
		{
			Module = module;
			Context = ctx;
		}
		public ClrType GetClrType(string TypeName)
		{
			ClrType type = Module.GetTypeByName(TypeName);
			if (type is null)
				throw MakeArgNotFoundException<ClrType>("TypeName", TypeName);
			return type;
		}

		public ClrMethod GetClrMethod(string TypeName, string MethodName)
		{
			ClrMethod[] methods = GetClrType(TypeName).MethodsInVTable.Where(t => t.Name == MethodName).ToArray();
			if (methods.Length == 0)
				throw MakeArgNotFoundException<ClrMethod>("MethodName", MethodName);
			return methods[0];
		}

		public ClrMethod GetClrMethod(string TypeName, Func<ClrMethod, bool> filter) => GetClrType(TypeName).MethodsInVTable.First(t => filter(t));

		public nuint GetFunctionAddress(string TypeName, string FunctionName) => GetClrMethod(TypeName, FunctionName).NativeCode;
		public nuint GetFunctionAddress(string TypeName, Func<ClrMethod, bool> filter) => GetClrMethod(TypeName, t => filter(t)).NativeCode;

		//public ILToNativeMap GetFunctionInstruction(string TypeName, string FunctionName, int ILOffset) => GetClrType(TypeName).MethodsInVTable.First(t => t.Name == FunctionName).ILOffsetMap.First(t => t.ILOffset == ILOffset);

		public int GetStaticFieldAddress(string typeName, string fieldName)
		{
			ClrStaticField field = GetClrType(typeName).GetStaticFieldByName(fieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrStaticField>("FieldName", fieldName);
			return (int)field.GetAddress();
		}

		public int GetFieldOffset(string typeName, string fieldName)
		{
			ClrInstanceField field = GetClrType(typeName).GetInstanceFieldByName(fieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrInstanceField>("FieldName", fieldName);
			return (int)field.Offset + 4;//+4 to get true offset
		}

		public T GetStaticFieldValue<T>(string typeName, string fieldName) where T : unmanaged
		{
			ClrStaticField field = GetClrType(typeName).GetStaticFieldByName(fieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrStaticField>(nameof(fieldName), fieldName);
			return Context.DataAccess.Read<T>(field.GetAddress());
		}
		public void SetStaticFieldValue<T>(string typeName, string fieldName, T value) where T : unmanaged
		{
			ClrStaticField field = GetClrType(typeName).GetStaticFieldByName(fieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrStaticField>(nameof(fieldName), fieldName);
			Context.DataAccess.Write(field.GetAddress(), value);
		}

		public T GetInstanceFieldValue<T>(string typeName, string fieldName, nuint obj) where T : unmanaged
		{
			ClrInstanceField field = GetClrType(typeName).GetInstanceFieldByName(fieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrInstanceField>(nameof(fieldName), fieldName);
			return Context.DataAccess.Read<T>(field.GetAddress(obj));
		}

		public void SetInstanceFieldValue<T>(string typeName, string fieldName, nuint obj, T value) where T : unmanaged
		{
			ClrInstanceField field = GetClrType(typeName).GetInstanceFieldByName(fieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrInstanceField>(nameof(fieldName), fieldName);
			Context.DataAccess.Write<T>(field.GetAddress(obj), value);
		}

		public HackObject GetStaticHackObject(string typeName, string fieldName) =>
			new(Context, GetClrType(typeName).GetStaticFieldByName(fieldName).GetValue());

		public T GetStaticHackObjectValue<T>(string typeName, string fieldName) where T : unmanaged =>
			GetClrType(typeName).GetStaticFieldByName(fieldName).GetRawValue<T>();

		public void SetStaticHackObject<T>(string typeName, string fieldName, T value) where T : HackObject
		{
			ClrType type = GetClrType(typeName);
			ClrStaticField field = type.GetStaticFieldByName(fieldName);
			if (!value.ClrType.IsPrimitive && value.ClrType != field.Type)
				throw new ClrTypeNotMatchedException("Ref type not matched.", nameof(value));
			nuint addr = field.GetAddress();
			if (field.Type.IsPrimitive)
				Context.DataAccess.WriteBytes(addr, Context.DataAccess.ReadBytes(value.BaseAddress, (int)value.ClrType.BaseSize - sizeof(nuint) * 2));
			else
				Context.DataAccess.Write(addr, Context.DataAccess.Read<IntPtr>(value.BaseAddress));
		}

		public void SetStaticHackObjectValue<T>(string typeName, string fieldName, T value) where T : unmanaged
		{
			ClrType type = GetClrType(typeName);
			ClrStaticField field = type.GetStaticFieldByName(fieldName);
			if (!field.Type.IsPrimitive)
				throw new ClrTypeNotMatchedException("Ref type not matched.", nameof(fieldName));
			Context.DataAccess.Write<T>(field.GetAddress(), value);
		}

		private Exception MakeArgNotFoundException<T>(string fieldName, string fieldValue)
		{
			Type type = typeof(T);
			if (type.IsSubclassOf(typeof(ClrType)))
				return new ClrTypeNotFoundException($"No such type found: {fieldValue}", fieldName);
			else if (type.IsSubclassOf(typeof(ClrStaticField)))
				return new ClrTypeNotFoundException($"No such static field found: {fieldValue}", fieldName);
			else if (type.IsSubclassOf(typeof(ClrInstanceField)))
				return new ClrTypeNotFoundException($"No such static field found: {fieldValue}", fieldName);
			else if (type.IsSubclassOf(typeof(ClrMethod)))
				return new ClrTypeNotFoundException($"No such method found: {fieldValue}", fieldName);
			return new ArgumentException($"No such {typeof(T).Name} found", fieldName);
		}

		internal class ClrTypeNotMatchedException : ArgumentException
		{
			public ClrTypeNotMatchedException(string msg, string param) : base(msg, param) { }
		}
		internal class ClrTypeNotFoundException : ArgumentException
		{
			public ClrTypeNotFoundException(string msg, string param) : base(msg, param) { }
		}
		internal class ClrMethodNotFoundException : ArgumentException
		{
			public ClrMethodNotFoundException(string msg, string param) : base(msg, param) { }
		}
		internal abstract class ClrFieldNotFoundException : ArgumentException
		{
			public ClrFieldNotFoundException(string msg, string param) : base(msg, param) { }
		}
		internal class ClrStaticFieldNotFoundException : ClrFieldNotFoundException
		{
			public ClrStaticFieldNotFoundException(string msg, string param) : base(msg, param) { }
		}
		internal class ClrInstanceFieldNotFoundException : ClrFieldNotFoundException
		{
			public ClrInstanceFieldNotFoundException(string msg, string param) : base(msg, param) { }
		}
	}
}
