using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackLib.Assemble
{
	public class Instruction : AssemblyCode
	{
		public string Code
		{
			get;
		}
		private Instruction(string code) => Code = code;
		public static Instruction Create(string code) => new(code);
		public override string GetCode() => Code;
		public override byte[] GetByteCode(nuint ip) => Assembler.Assemble(Code, ip);
		public static explicit operator Instruction(string s) => Create(s);
	}
}
