using QHackLib;
using QTRHack.Core.ValueTypeRedefs;
using QTRHack.Kernel.Interface;
using QTRHack.Kernel.Interface.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.GameObjects
{
	/// <summary>
	/// Wrapper for Terraria.Entity
	/// </summary>
	public abstract class BaseEntity : GameObject
	{
		protected BaseEntity(BaseCore core, HackObject obj) : base(core, obj)
		{
		}

		public virtual ValueTypeRedefs.Xna.Vector2 OldPosition { get => InternalObject.oldPosition; set => InternalObject.oldPosition = value; }
		public virtual ValueTypeRedefs.Xna.Vector2 OldVelocity { get => InternalObject.oldVelocity; set => InternalObject.oldVelocity = value; }
		public virtual ValueTypeRedefs.Xna.Vector2 Position { get => InternalObject.position; set => InternalObject.position = value; }
		public virtual ValueTypeRedefs.Xna.Vector2 Velocity { get => InternalObject.velocity; set => InternalObject.velocity = value; }
		public virtual bool Active { get => InternalObject.active; set => InternalObject.active = value; }
		public virtual bool HoneyWet { get => InternalObject.honeyWet; set => InternalObject.honeyWet = value; }
		public virtual bool LavaWet { get => InternalObject.lavaWet; set => InternalObject.lavaWet = value; }
		public virtual bool Wet { get => InternalObject.wet; set => InternalObject.wet = value; }
		public virtual byte WetCount { get => InternalObject.wetCount; set => InternalObject.wetCount = value; }
		public virtual int Direction { get => InternalObject.direction; set => InternalObject.direction = value; }
		public virtual int OldDirection { get => InternalObject.oldDirection; set => InternalObject.oldDirection = value; }
		public virtual int Height { get => InternalObject.height; set => InternalObject.height = value; }
		public virtual int WhoAmI { get => InternalObject.whoAmI; set => InternalObject.whoAmI = value; }
		public virtual int Width { get => InternalObject.width; set => InternalObject.width = value; }
		public virtual long EntityId { get => InternalObject.entityId; set => InternalObject.entityId = value; }
	}
}
