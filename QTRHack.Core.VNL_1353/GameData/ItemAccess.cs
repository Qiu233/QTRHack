using QTRHack.Kernel.Interface.GameData.Content;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.Core.VNL_1353.GameData
{
	public class ItemAccess : BaseItemAccess
	{
		public override Image OR_Icon(ItemAccessArgs arg)
		{
			throw new NotImplementedException();
		}

		public override object OR_Type(ItemAccessArgs arg)
		{
			dynamic item = arg.Item;
			return (int)item.type;
		}
	}
}
