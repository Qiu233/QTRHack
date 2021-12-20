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
	public unsafe class CLRHelper
	{
		public QHackContext Context { get; }
		public ClrModule Module { get; }
		public string ModuleName => Module.Name;
		public nuint this[string typeName, string FunctionName]
		{
			get => GetFunctionAddress(typeName, FunctionName);
		}
		/*public ILToNativeMap this[string typeName, string FunctionName, int ILOffset]
		{
			get => GetFunctionInstruction(typeName, FunctionName, ILOffset);
		}*/
		internal CLRHelper(QHackContext ctx, ClrModule module)
		{
			Module = module;
			Context = ctx;
		}
		public ClrType GetClrType(string typeName)
		{
			ClrType type = Module.GetTypeByName(typeName);
			if (type is null)
				throw MakeArgNotFoundException<ClrType>("typeName", typeName);
			return type;
		}

		public ClrMethod GetClrMethod(string typeName, string methodName)
		{
			ClrMethod[] methods = GetClrType(typeName).MethodsInVTable.Where(t => t.Name == methodName).ToArray();
			if (methods.Length == 0)
				throw MakeArgNotFoundException<ClrMethod>("methodName", methodName);
			return methods[0];
		}

		public ClrMethod GetClrMethod(string typeName, Func<ClrMethod, bool> filter) => GetClrType(typeName).MethodsInVTable.First(t => filter(t));

		public nuint GetFunctionAddress(string typeName, string FunctionName) => GetClrMethod(typeName, FunctionName).NativeCode;
		public nuint GetFunctionAddress(string typeName, Func<ClrMethod, bool> filter) => GetClrMethod(typeName, t => filter(t)).NativeCode;

		//public ILToNativeMap GetFunctionInstruction(string typeName, string FunctionName, int ILOffset) => GetClrType(typeName).MethodsInVTable.First(t => t.Name == FunctionName).ILOffsetMap.First(t => t.ILOffset == ILOffset);

		public ClrMethod GetClrMethodBySignature(string typeName, string signature) => GetClrMethod(typeName, m => m.Signature == signature);

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
				Context.DataAccess.WriteBytes(addr, Context.DataAccess.ReadBytes(value.BaseAddress, (uint)(value.ClrType.BaseSize - sizeof(nuint) * 2)));
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

		private static Exception MakeArgNotFoundException<T>(string fieldName, string fieldValue)
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
