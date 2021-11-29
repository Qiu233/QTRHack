using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface
{
	public enum GameType
	{
		NONE = 0,
		/// <summary>
		/// vanilla
		/// </summary>
		VNL,
		/// <summary>
		/// tModLoader
		/// </summary>
		TML,
		OTHER,
	}
	public sealed class CoreVersionSig
	{
		public GameType GameType
		{
			get;
		}
		public Version GameVersion
		{
			get;
		}
		public int Build
		{
			get;
		}

		public CoreVersionSig(GameType gameType, Version gameVersion, int build)
		{
			GameType = gameType;
			GameVersion = gameVersion;
			Build = build;
		}

		public override string ToString()
		{
			return $"{GameType}-{GameVersion}-{Build}";
		}


		public static bool TryParse(string value, out CoreVersionSig sig)
		{
			sig = new CoreVersionSig(GameType.NONE, Version.Parse("0.0.0.0"), 0);//default
			string[] s = value.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
			if (s.Length != 3) return false;
			if (!Enum.TryParse(s[0], out GameType gameType)) return false;
			if (!Version.TryParse(s[1], out Version gameVersion)) return false;
			if (!int.TryParse(s[2], out int build)) return false;
			sig = new CoreVersionSig(gameType, gameVersion, build);
			return true;
		}

		public static CoreVersionSig Parse(string value)
		{
			TryParse(value, out CoreVersionSig sig);
			return sig;
		}

		public static bool operator ==(CoreVersionSig a, CoreVersionSig b)
		{
			if (a is null)
				return b is null;
			return a.Equals(b);
		}
		public static bool operator !=(CoreVersionSig a, CoreVersionSig b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			if (obj is null)
				return false;
			if (!(obj is CoreVersionSig))
				return false;
			if (GetHashCode() == obj.GetHashCode())
				return true;
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			int hashCode = -1116190013;
			hashCode = hashCode * -1521134295 + GameType.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<Version>.Default.GetHashCode(GameVersion);
			hashCode = hashCode * -1521134295 + Build.GetHashCode();
			return hashCode;
		}
	}
}
