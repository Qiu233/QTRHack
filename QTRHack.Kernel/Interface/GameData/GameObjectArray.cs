﻿using QHackLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	/// <summary>
	/// For array of ref types.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class GameObjectArray<T> : GameObject, IEnumerable<T> where T : GameObject
	{
		public int Length => TypedInternalObject.GetArrayLength();
		public T this[int index]
		{
			get => Core.MakeGameDataAccess<T>(InternalObject[index]);
			set => InternalObject[index] = value.InternalObject;
		}
		public GameObjectArray(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public IEnumerator GetEnumerator() => (this as IEnumerable<T>).GetEnumerator();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new GameObjectArrayEnumerator(this);
		private sealed class GameObjectArrayEnumerator : IEnumerator<T>
		{
			T IEnumerator<T>.Current => Data[Index];
			public object Current => (this as IEnumerator<T>).Current;
			public GameObjectArray<T> Data { get; }
			private int Index = -1;
			public GameObjectArrayEnumerator(GameObjectArray<T> data) => Data = data;
			public bool MoveNext() => ++Index < Data.Length;
			public void Reset() => Index = -1;
			public void Dispose() { }
		}
	}
	/// <summary>
	/// For fast access unmanaged types.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class GameObjectArrayV<T> : GameObject, IEnumerable<T> where T : unmanaged
	{
		public int Length => TypedInternalObject.GetArrayLength();
		public T this[int index]
		{
			get => (T)InternalObject[index];
			set => InternalObject[index] = value;
		}
		public GameObjectArrayV(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public IEnumerator GetEnumerator() => (this as IEnumerable<T>).GetEnumerator();
		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new GameObjectArrayEnumerator(this);
		private sealed class GameObjectArrayEnumerator : IEnumerator<T>
		{
			T IEnumerator<T>.Current => Data[Index];
			public object Current => (this as IEnumerator<T>).Current;
			public GameObjectArrayV<T> Data { get; }
			private int Index = -1;
			public GameObjectArrayEnumerator(GameObjectArrayV<T> data) => Data = data;
			public bool MoveNext() => ++Index < Data.Length;
			public void Reset() => Index = -1;
			public void Dispose() { }
		}
	}
	/// <summary>
	/// For when no GameObject wrapper is implemented.
	/// </summary>
	public sealed class GameObjectArray : GameObject, IEnumerable
	{
		public int Length => TypedInternalObject.GetArrayLength();
		public dynamic this[int index]
		{
			get => InternalObject[index];
			set => InternalObject[index] = value;
		}
		public GameObjectArray(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public IEnumerator GetEnumerator() => new GameObjectArrayEnumerator(this);
		private sealed class GameObjectArrayEnumerator : IEnumerator
		{
			public object Current => Data[Index];
			public GameObjectArray Data { get; }
			private int Index = -1;
			public GameObjectArrayEnumerator(GameObjectArray data) => Data = data;
			public bool MoveNext() => ++Index < Data.Length;
			public void Reset() => Index = -1;
		}
	}
}