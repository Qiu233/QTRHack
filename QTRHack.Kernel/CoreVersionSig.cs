using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel
{
	public enum GameType
	{
		NONE = 0,
		VNL,
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
	}
}
