using Keystone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QHackLib.Assemble
{
	public sealed class Assembler
	{
		private readonly List<byte> InternalData;
		public IReadOnlyList<byte> Data
		{
			get => InternalData;
		}
		private nuint _IP;
		public nuint IP => _IP;
		public Assembler(nuint IP)
		{
			_IP = IP;
			InternalData = new List<byte>();
		}

		/// <summary>
		/// This method is thread safe.
		/// </summary>
		/// <param name="data"></param>
		public void Emit(byte[] data)
		{
			lock (InternalData)
			{
				InternalData.AddRange(data);
				_IP += (uint)data.Length;
			}
		}
		public void Assemble(string code) => Emit(Assemble(code, IP));
		public void Assemble(AssemblyCode code) => Assemble(code.GetCode());
		public byte[] GetByteCode() => InternalData.ToArray();


		public unsafe static byte[] Assemble(string code, nuint IP)
		{
			using Engine keystone = new(Keystone.Architecture.X86, Mode.X32) { ThrowOnError = true };
			EncodedData enc = keystone.Assemble(code, IP);
			return enc.Buffer;
		}
	}
}
