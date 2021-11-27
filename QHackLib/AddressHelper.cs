using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib
{
	public class AddressHelper
	{
		public Context Context { get; }
		public ClrModule Module { get; }
		public string ModuleName { get => Module.Name; }
		public int this[string TypeName, string FunctionName]
		{
			get => GetFunctionAddress(TypeName, FunctionName);
		}
		public ILToNativeMap this[string TypeName, string FunctionName, int ILOffset]
		{
			get => GetFunctionInstruction(TypeName, FunctionName, ILOffset);
		}
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
			ClrMethod[] methods = GetClrType(TypeName).Methods.Where(t => t.Name == MethodName).ToArray();
			if (methods.Length == 0)
				throw MakeArgNotFoundException<ClrMethod>("MethodName", MethodName);
			return methods[0];
		}

		public ClrMethod GetClrMethod(string TypeName, Func<ClrMethod, bool> filter) => GetClrType(TypeName).Methods.First(t => filter(t));

		public int GetFunctionAddress(string TypeName, string FunctionName) => (int)GetClrMethod(TypeName, FunctionName).NativeCode;
		public int GetFunctionAddress(string TypeName, Func<ClrMethod, bool> filter) => (int)GetClrMethod(TypeName, t => filter(t)).NativeCode;

		public ILToNativeMap GetFunctionInstruction(string TypeName, string FunctionName, int ILOffset) => GetClrType(TypeName).Methods.First(t => t.Name == FunctionName).ILOffsetMap.First(t => t.ILOffset == ILOffset);

		public int GetStaticFieldAddress(string TypeName, string FieldName)
		{
			ClrStaticField field = GetClrType(TypeName).GetStaticFieldByName(FieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrStaticField>("FieldName", FieldName);
			return (int)field.GetAddress(Module.AppDomain);
		}

		public int GetFieldOffset(string TypeName, string FieldName)
		{
			ClrInstanceField field = GetClrType(TypeName).GetFieldByName(FieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrInstanceField>("FieldName", FieldName);
			return field.Offset + 4;//+4 to get true offset
		}

		public T GetStaticFieldValue<T>(string TypeName, string FieldName) where T : unmanaged
		{
			ClrStaticField field = GetClrType(TypeName).GetStaticFieldByName(FieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrStaticField>("FieldName", FieldName);
			return Context.DataAccess.Read<T>((int)field.GetAddress(Module.AppDomain));
		}
		public void SetStaticFieldValue<T>(string TypeName, string FieldName, T value) where T : unmanaged
		{
			ClrStaticField field = GetClrType(TypeName).GetStaticFieldByName(FieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrStaticField>("FieldName", FieldName);
			Context.DataAccess.Write((int)field.GetAddress(Module.AppDomain), value);
		}

		public T GetInstanceFieldValue<T>(string TypeName, string FieldName, int obj) where T : unmanaged
		{
			ClrInstanceField field = GetClrType(TypeName).GetFieldByName(FieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrInstanceField>("FieldName", FieldName);
			return Context.DataAccess.Read<T>((int)field.GetAddress((ulong)obj));
		}

		public void SetInstanceFieldValue<T>(string TypeName, string FieldName, int obj, T value) where T : unmanaged
		{
			ClrInstanceField field = GetClrType(TypeName).GetFieldByName(FieldName);
			if (field is null)
				throw MakeArgNotFoundException<ClrInstanceField>("FieldName", FieldName);
			Context.DataAccess.Write<T>((int)field.GetAddress((ulong)obj), value);
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
