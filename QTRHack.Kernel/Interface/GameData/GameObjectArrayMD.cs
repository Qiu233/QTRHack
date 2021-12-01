using QHackLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	/// <summary>
	/// For multidimensional arrays.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GameObjectArrayMD<T> : GameObject where T : GameObject
	{
		public int Rank =>
			TypedInternalObject.GetArrayRank();
		public int GetLength(int dimension) =>
			TypedInternalObject.GetArrayLength(dimension);
		public T GetValue(params int[] indexes) => 
			Core.MakeGameDataAccess<T>(TypedInternalObject.InternalGetIndex(indexes.Select(t => (object)t).ToArray()));
		public void SetValue(T value, params int[] indexes) => 
			TypedInternalObject.InternalSetIndex(indexes.Select(t => (object)t).ToArray(), value.InternalObject);

		public GameObjectArrayMD(BaseCore core, HackObject obj) : base(core, obj)
		{
		}
	}
	/// <summary>
	/// For fast access to unmanaged types.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class GameObjectArrayMDV<T> : GameObject where T : unmanaged
	{
		public int Rank =>
			TypedInternalObject.GetArrayRank();
		public int GetLength(int dimension) =>
			TypedInternalObject.GetArrayLength(dimension);
		public T GetValue(params int[] indexes) =>
			(T)(dynamic)TypedInternalObject.InternalGetIndex(indexes.Select(t => (object)t).ToArray());
		public void SetValue(T value, params int[] indexes) =>
			TypedInternalObject.InternalSetIndex(indexes.Select(t => (object)t).ToArray(), value);
		public GameObjectArrayMDV(BaseCore core, HackObject obj) : base(core, obj)
		{
		}
	}
	public class GameObjectArrayMD : GameObject
	{
		public int Rank =>
			TypedInternalObject.GetArrayRank();
		public int GetLength(int dimension) =>
			TypedInternalObject.GetArrayLength(dimension);
		public dynamic GetValue(params int[] indexes) =>
			TypedInternalObject.InternalGetIndex(indexes.Select(t => (object)t).ToArray());
		public void SetValue(dynamic value, params int[] indexes) =>
			TypedInternalObject.InternalSetIndex(indexes.Select(t => (object)t).ToArray(), value);

		public GameObjectArrayMD(BaseCore core, HackObject obj) : base(core, obj)
		{
		}
	}
}
