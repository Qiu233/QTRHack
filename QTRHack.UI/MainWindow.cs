using MahApps.Metro.Controls;
using QTRHack.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTRHack.UI
{
	public class MainWindow : MetroWindow
	{
		/// <summary>
		/// one kernel for one MainWindow
		/// </summary>
		public HackKernel HackKernel
		{
			get;
		}
		public MainWindow(HackKernel kernel)
		{
			HackKernel = kernel;
			Width = 500;
			Height = 400;
			WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
			ResizeMode = System.Windows.ResizeMode.NoResize;
		}
	}
}
