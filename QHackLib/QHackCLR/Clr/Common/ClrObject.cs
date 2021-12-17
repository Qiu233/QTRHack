using QHackCLR.Dac.Interfaces;
using QHackCLR.Dac.Utils;
using QHackCLR.DataTargets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QHackCLR.Clr
{
	public unsafe sealed class ClrObject : AddressableTypedEntity
	{
		public readonly DacpObjectData Data;
		public ClrObject(ClrType type, nuint address) : base(type, address)
		{
			ClrObjectHelper.SOSDac.GetObjectData(address, out Data);
		}
		public bool IsArray => Type.IsArray;
		public ulong Size => Data.Size;

		public int GetLength()
		{
			if (!IsArray)
				throw new InvalidOperationException("Not an array");
			return Read<int>(sizeof(nuint));
		}

		public int GetLength(int dimension)
		{
			int rank = Type.Rank;
			if (dimension >= rank)
				throw new ArgumentOutOfRangeException(nameof(dimension));
			if (Type.CorElementType == CorElementType.SZArray)//SZArray
				return GetLength();
			return Read<int>(sizeof(nuint) * 2 + sizeof(int) * dimension);
		}

		public int GetLowerBound(int dimension)
		{
			int rank = Type.Rank;
			if (dimension >= rank)
				throw new ArgumentOutOfRangeException(nameof(dimension));
			if (rank == 1)
				return 0;
			return Read<int>(sizeof(nuint) * 2 + sizeof(int) * (rank + dimension));
		}

		public int GetArrayElementOffset(params int[] indices)
		{
			int rank = Type.Rank;
			if (indices.Length != rank)
				throw new ArgumentException("Rank does not match", nameof(indices));
			if (Type.CorElementType == CorElementType.SZArray)
				return sizeof(nuint) * 2 + (indices[0] * (int)Type.ComponentSize);
			int offset = 0;
			for (int i = 0; i < rank; i++)
			{
				int currentValueOffset = indices[i] - GetLowerBound(i);
				if ((uint)currentValueOffset >= GetLength(i))
					throw new ArgumentOutOfRangeException(nameof(indices));
				offset *= GetLength(i);
				offset += currentValueOffset;
			}
			return sizeof(nuint) * 2 + (8 * rank) + (int)(offset * Type.ComponentSize);
		}

		public nuint GetArrayElementAddress(params int[] indices) => Address + (nuint)GetArrayElementOffset(indices);

		public AddressableTypedEntity GetArrayElement(params int[] indices)
		{
			if (!Type.IsArray)
				throw new InvalidOperationException("Not an array");
			int offset = GetArrayElementOffset(indices);
			ClrType componentType = Type.ComponentType;
			if (componentType.IsValueType)
				return new ClrValue(componentType, Address + (uint)offset);
			return new ClrObject(componentType, Read<nuint>(offset));
		}

		public T ReadArrayElement<T>(params int[] indices) where T : unmanaged => Read<T>(GetArrayElementOffset(indices));
		public AddressableTypedEntity GetArrayElement(int index) => GetArrayElement(new int[] { index });
	}
}
// |<-----------Object-Layout------------->|
// | sync_block | method_table | fields... |
// |  pointer   |   pointer    |           |
// |____________|______________|___________|
//              ^ref           ^offset base

// Things become complicated for arrays.

// SZArray (string is special SZArray of char)
// |<------Fields-Layout------>|
// |   length   |  elements... |
// |   pointer  |              |
// |____________|______________|
// ^offset base

// Array
// |<-------------------Fields-Layout-------------------->|
// |  length  |  lengths   |  lowerbounds  |  elements... |
// |  pointer |  int[rank] |   int[rank]   |              |
// |__________|____________|_______________|______________|
// ^offset base                            ^where elements begin