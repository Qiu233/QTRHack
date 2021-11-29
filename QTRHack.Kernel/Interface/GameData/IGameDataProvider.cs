using QHackLib;
using QTRHack.Kernel.Interface.GameData.Content;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Kernel.Interface.GameData
{
	/// <summary>
	/// Defines the interface to request data of game.<br/>
	/// Note that this class has only access to basic game data which works for all types of game.<br/>
	/// User DataAccess should be defined in the implementation of this interface, 
	/// otherwise the <see cref="IGameDataProvider.FallbackHandler{V}(GDAccessArgs)"/> will be the default handler for that type of args.
	/// </summary>
	public interface IGameDataProvider
	{
		BaseItemAccess ItemAccess { get; }
		BasePlayerAccess PlayerAccess { get; }
	}
}
