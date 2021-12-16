using QHackCLR.DataTargets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackCLR.Clr
{
	public sealed class ClrValue : AddressableTypedEntity
	{
		public ClrValue(ClrType type, nuint address) : base(type, address)
		{
		}

		/// <summary>
		/// You can get value from this entity as <typeparamref name="T"/> 
		/// only when sizeof(<typeparamref name="T"/>) is less than size of this Type.<br/>
		/// Otherwise an exception will be thrown.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public unsafe T GetValue<T>() where T : unmanaged
		{
			if (Type.UserSize > sizeof(T))
				throw new InvalidOperationException("Size exceeded.");
			return Read<T>(0);
		}

		/// <summary>
		/// Reads bytes with unchecked size.
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public byte[] ReadBytes(int size) => DataAccess.ReadBytes(Address, size);

		/// <summary>
		/// Reads bytes with default size.
		/// </summary>
		/// <returns></returns>
		public byte[] ReadBytes() => DataAccess.ReadBytes(Address, (int)Type.BaseSize - IntPtr.Size);
	}
}
