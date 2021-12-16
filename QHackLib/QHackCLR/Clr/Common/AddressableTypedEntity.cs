using QHackCLR.Clr.Builders.Helpers;
using QHackCLR.DataTargets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackCLR.Clr
{
	public abstract class AddressableTypedEntity : IEquatable<AddressableTypedEntity>
	{
		public ClrType Type { get; }
		public nuint Address { get; }
		public DataAccess DataAccess => ClrObjectHelper.DataAccess;
		protected IClrObjectHelper ClrObjectHelper => Type.ClrObjectHelper;

		protected AddressableTypedEntity(ClrType type, nuint address)
		{
			Type = type;
			Address = address;
		}

		public T Read<T>(int offset) where T : unmanaged => DataAccess.Read<T>(Address + (uint)offset);
		public void Write<T>(int offset, T value) where T : unmanaged => DataAccess.Write(Address + (uint)offset, value);

		/// <summary>
		/// Only for instance fields.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public AddressableTypedEntity GetFieldValue(string name)
		{
			ClrInstanceField field = Type.EnumerateInstanceFields().FirstOrDefault(t => t.Name == name) ??
				throw new ArgumentException("No such field", nameof(name));
			nuint addr = field.GetAddress(Address);
			return field.GetValue(addr);
		}
		public T GetFieldValue<T>(string name) where T : AddressableTypedEntity => GetFieldValue(name) as T;
		public bool Equals(AddressableTypedEntity other) => this == other;
		public override bool Equals(object obj) => Equals(obj as AddressableTypedEntity);
		public override int GetHashCode() => (int)Address;
		public static bool operator !=(AddressableTypedEntity a, AddressableTypedEntity b) => !(a == b);
		public static bool operator ==(AddressableTypedEntity a, AddressableTypedEntity b)
		{
			if (b is null)
				return a is null;
			return a.Equals(b);
		}
	}
}
