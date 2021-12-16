using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib.Assemble
{
	public class AssemblySnippet : AssemblyCode
	{
		#region CLRCall
		public static bool IsPTypePassingMustOnStack(string typeName)
		{
			switch (typeName)
			{
				case "System.Int64":
				case "System.UInt64":
				case "System.Single":
				case "System.Double":
					return true;
				default:
					return false;
			}
		}

		private unsafe static object[] ProcessUserArgs(object[] userArgs)
		{
			List<object> processedUserArgs = new();
			foreach (var arg in userArgs)
			{
				Type type = arg.GetType();
				if (!type.IsValueType)
					throw new ClrArgsPassingException($"Can only pass game object and value. Type: {type.FullName}");
				if (type.IsPrimitive)
				{
					processedUserArgs.Add(arg);//normal
				}
				else
				{
					int size = Marshal.SizeOf(type);
					byte[] data = new byte[size];
					IntPtr ptr = Marshal.AllocHGlobal(size);
					Marshal.StructureToPtr(arg, ptr, false);
					Marshal.Copy(ptr, data, 0, size);
					Marshal.FreeHGlobal(ptr);
					processedUserArgs.Add(data);//normal
				}
			}
			return processedUserArgs.ToArray();
		}

		private static AssemblyCode GetArugumentsPassing(int? thisPtr, int? retBuf, object[] userArgs)
		{
			AssemblySnippet snippet = new();
			int index = 0;
			int reg = 0;
			int stack = 0;
			object[] args = ProcessUserArgs(userArgs);
			if (thisPtr != null)
				snippet.Content.Add((Instruction)$"mov {(reg++ == 0 ? "ecx" : "edx")},{thisPtr.Value}");
			if (retBuf != null)
				snippet.Content.Add((Instruction)$"mov {(reg++ == 0 ? "ecx" : "edx")},{retBuf.Value}");
			foreach (var arg in args)
			{
				Type type = arg.GetType();
				if (type == typeof(byte[]))
				{
					byte[] data = arg as byte[];
					int count = (data.Length + 3) / 4;
					byte[] targetData = ArrayPool<byte>.Shared.Rent(count * 4);
					Array.Clear(targetData, 0, targetData.Length);//0
					Array.Copy(data, targetData, data.Length);
					for (int i = 0; i < count; i++)
						snippet.Content.Add((Instruction)$"push {BitConverter.ToUInt32(targetData, (count - i - 1) * 4)}");
					stack += count;
					ArrayPool<byte>.Shared.Return(targetData);
				}
				else if (type.IsPrimitive)
				{
					if (IsPTypePassingMustOnStack(type.Name))
					{
						if (type.Name == "System.Int64" || type.Name == "System.UInt64")
						{
							byte[] data = BitConverter.GetBytes((ulong)arg);
							uint low = BitConverter.ToUInt32(data, 0);
							uint high = BitConverter.ToUInt32(data, 32);
							snippet.Content.Add((Instruction)$"push {high}");
							snippet.Content.Add((Instruction)$"push {low}");
							stack += 2;
						}
						else if (type.Name == "System.Double")
						{
							byte[] data = BitConverter.GetBytes((double)arg);
							uint low = BitConverter.ToUInt32(data, 0);
							uint high = BitConverter.ToUInt32(data, 32);
							snippet.Content.Add((Instruction)$"push {high}");
							snippet.Content.Add((Instruction)$"push {low}");
							stack += 2;
						}
						else//float
						{
							byte[] data = BitConverter.GetBytes((float)arg);
							snippet.Content.Add((Instruction)$"push {BitConverter.ToUInt32(data, 0)}");
							stack++;
						}
					}
					else
					{
						uint value = Convert.ToUInt32(arg);
						if (reg < 2)
						{
							snippet.Content.Add((Instruction)$"mov {(reg++ == 0 ? "ecx" : "edx")},{value}");
						}
						else
						{
							snippet.Content.Add((Instruction)$"push {value}");
							stack++;
						}
					}
				}
				else//ref
					throw new ClrArgsPassingException($"Can only pass game value and byte[]. Type: {type.FullName}");
				index++;
			}
			return snippet;
		}

		public static AssemblySnippet FromClrCall(int targetAddr, bool regProtection, int? thisPtr, int? retBuf, params object[] arguments)
		{
			AssemblySnippet s = new();
			if (regProtection)
			{
				s.Content.Add(Instruction.Create("push ecx"));
				s.Content.Add(Instruction.Create("push edx"));
			}
			s.Content.Add(GetArugumentsPassing(thisPtr, retBuf, arguments));
			s.Content.Add(Instruction.Create($"call {targetAddr}"));
			if (regProtection)
			{
				s.Content.Add(Instruction.Create("pop edx"));
				s.Content.Add(Instruction.Create("pop ecx"));
			}
			return s;
		}
		#endregion

		#region Thread
		public static AssemblySnippet StartManagedThread(Context ctx, int lpCodeAddr, int lpwStrName_System_Action)
		{
			int ctorCharPtrMethod = ctx.BCLAddressHelper.GetFunctionAddress("System.String", "CtorCharPtr");
			int getTypeMethod = ctx.BCLAddressHelper.GetFunctionAddress("System.Type",
				t => t.Signature == "System.Type.GetType(System.String)");
			int getPtrMethod = ctx.BCLAddressHelper.GetFunctionAddress("System.Runtime.InteropServices.Marshal",
				t => t.Signature == "System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer(IntPtr, System.Type)");
			int taskRunMethod = ctx.BCLAddressHelper.GetFunctionAddress("System.Threading.Tasks.Task",
				t => t.Signature == "System.Threading.Tasks.Task.Run(System.Action)");
			return FromCode(
					new AssemblyCode[] {
						(Instruction)"pushad",
						FromClrCall(ctorCharPtrMethod,false,0,null,lpwStrName_System_Action),
						(Instruction)"mov ecx,eax",
						(Instruction)$"call {getTypeMethod}",
						(Instruction)$"mov ecx,{lpCodeAddr}",
						(Instruction)"mov edx,eax",
						(Instruction)$"call {getPtrMethod}",
						(Instruction)"mov ecx,eax",
						(Instruction)$"call {taskRunMethod}",
						(Instruction)"popad",
				});
		}
		#endregion

		private static readonly Random _random = new();
		public List<AssemblyCode> Content
		{
			get;
		}

		private AssemblySnippet() => Content = new List<AssemblyCode>();

		public static AssemblySnippet FromEmpty() => new();

		public static AssemblySnippet FromASMCode(string asm)
		{
			AssemblySnippet s = new();
			s.Content.AddRange(
				asm.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(t => Instruction.Create(t)));
			return s;
		}

		public static AssemblySnippet FromCode(IEnumerable<AssemblyCode> code)
		{
			AssemblySnippet s = new();
			s.Content.AddRange(code);
			return s;
		}
		/// <summary>
		/// inside loop,[esp] is the iterator
		/// ecx will be changed
		/// </summary>
		/// <param name="body"></param>
		/// <param name="times"></param>
		/// <param name="regProtection"></param>
		/// <returns></returns>
		public static AssemblySnippet Loop(AssemblySnippet body, int times, bool regProtection)
		{
			byte[] lA = new byte[16];
			byte[] lB = new byte[16];
			_random.NextBytes(lA);
			_random.NextBytes(lB);
			AssemblySnippet s = new AssemblySnippet();
			string labelA = "lab_" + string.Concat(lA.Select(t => t.ToString("x2")));
			string labelB = "lab_" + string.Concat(lB.Select(t => t.ToString("x2")));
			if (regProtection)
				s.Content.Add(Instruction.Create("push ecx"));
			s.Content.Add(Instruction.Create("mov ecx,0"));
			s.Content.Add(Instruction.Create("" + labelA + ":"));
			s.Content.Add(Instruction.Create("cmp ecx," + times + ""));
			s.Content.Add(Instruction.Create("jge " + labelB + ""));
			s.Content.Add(Instruction.Create("push ecx"));
			s.Content.Add(body);
			s.Content.Add(Instruction.Create("pop ecx"));
			s.Content.Add(Instruction.Create("inc ecx"));
			s.Content.Add(Instruction.Create("jmp " + labelA + ""));
			s.Content.Add(Instruction.Create("" + labelB + ":"));
			if (regProtection)
				s.Content.Add(Instruction.Create("pop ecx"));
			return s;
		}

		/// <summary>
		/// Constructs a string from unicode wchar_t*.<br/>
		/// Naked call.<br/>
		/// ecx,edx,eax will be changed.<br/>
		/// eax will keep the return value.
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="strMemPtr">char* pointer of the string to be constructed</param>
		/// <param name="retPtr">the pointer to receive the result</param>
		/// <returns></returns>
		public static AssemblySnippet FromConstructString(Context ctx, int strMemPtr)
		{
			int ctor = ctx.BCLAddressHelper.GetFunctionAddress("System.String", "CtorCharPtr");
			return FromClrCall(ctor, false, 0, null, strMemPtr);
		}

		/// <summary>
		/// Loads an assembly.<br/>
		/// Naked call.<br/>
		/// ecx,eax will be changed.<br/>
		/// eax will keep the return value(pointer to Assembly).
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="assemblyFileNamePtr">string object containing assembly file name</param>
		/// <returns></returns>
		public static AssemblySnippet FromLoadAssembly(Context ctx, int assemblyFileNamePtr)
		{
			int loadFrom = ctx.BCLAddressHelper.GetFunctionAddress("System.Reflection.Assembly", "LoadFrom");
			return FromClrCall(loadFrom, false, null, null, assemblyFileNamePtr);
		}

		public override string GetCode() => string.Join('\n', Content);
		public override byte[] GetByteCode(nuint IP) => Assembler.Assemble(GetCode(), IP);

		public AssemblySnippet Copy()
		{
			AssemblySnippet ss = new();
			ss.Content.AddRange(Content);
			return ss;
		}

		internal class ClrArgsPassingException : Exception
		{
			public ClrArgsPassingException(string msg) : base(msg) { }
		}
	}
}
