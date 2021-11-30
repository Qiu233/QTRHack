using MahApps.Metro.Controls;
using QTRHack.Kernel;
using QTRHack.Kernel.Interface;
using QTRHack.UI.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QTRHack.UI
{
	public class StartupWindow : MetroWindow
	{
		[DllImport("User32.dll")]
		private static extern IntPtr WindowFromPoint(uint x, uint y);
		[DllImport("User32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);
		private readonly Grid ContentGrid, MainGrid;
		private static readonly string[] Languages = new string[] { "en-US", "zh-CN" };
		private const string DIR_CORES = "./Cores/";
		private readonly Dictionary<string, BaseCore> Cores = new Dictionary<string, BaseCore>();
		private readonly Button ConfirmButton, ResetButton;
		private readonly ListView CoresList;
		private readonly StatusBarItem StatusItem;
		private readonly DraggableCross DraggableCross;
		private int PID;
		public StartupWindow()
		{
			Width = 400;
			Height = 223;

			WindowStartupLocation = WindowStartupLocation.CenterScreen;
			Title = "QTRHack";
			ResizeMode = ResizeMode.NoResize;

			ContentGrid = new Grid();
			Content = ContentGrid;
			ContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(160) });
			ContentGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });

			MainGrid = new Grid();
			Grid.SetRow(MainGrid, 0);
			ContentGrid.Children.Add(MainGrid);
			MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(320) });
			MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(80) });

			StatusBar statusBar = new StatusBar()
			{
				Margin = new Thickness(1, 3, 1, 0),
			};
			StatusItem = new StatusBarItem();
			StatusItem.SetResourceReference(ContentProperty, "Localization.Control.SELECT_CORE");
			statusBar.Items.Add(StatusItem);
			Grid.SetRow(statusBar, 1);
			ContentGrid.Children.Add(statusBar);

			CoresList = new ListView
			{
				Margin = new Thickness(1, 2, 1, 0),
				BorderThickness = new Thickness(1),
				SelectionMode = SelectionMode.Single,
				BorderBrush = BorderBrush,
			};
			CoresList.SelectionChanged += (s, a) =>
			{
				UpdateStatus();
			};
			MainGrid.Children.Add(CoresList);
			Grid.SetColumn(CoresList, 0);

			Grid rightGrid = new Grid();
			MainGrid.Children.Add(rightGrid);
			Grid.SetColumn(rightGrid, 1);
			rightGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(70) });
			rightGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
			rightGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
			rightGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });

			DraggableCross = new DraggableCross
			{
				Width = 30,
				Height = 30,
				Margin = new Thickness(0, 0, 0, 0),
				IsEnabled = false
			};
			rightGrid.Children.Add(DraggableCross);
			Grid.SetRow(DraggableCross, 0);
			DraggableCross.OnCrossRelease += (p) =>
			{
				uint X = (uint)DraggableCross.PointToScreen(p).X;
				uint Y = (uint)DraggableCross.PointToScreen(p).Y;
				IntPtr window = WindowFromPoint(X, Y);
				GetWindowThreadProcessId(window, out int pid);
				PID = pid;
				UpdateStatus();
			};


			ComboBox languageComboBox = new ComboBox
			{
				Margin = new Thickness(0, 0, 3, 1),
				ItemsSource = Languages,
			};
			languageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
			languageComboBox.SelectedIndex = 0;
			rightGrid.Children.Add(languageComboBox);
			Grid.SetRow(languageComboBox, 1);


			ResetButton = new Button
			{
				BorderThickness = new Thickness(2),
				Margin = new Thickness(0, 1, 2, 0),
			};
			ResetButton.Click += ResetButton_Click;
			ResetButton.SetResourceReference(ContentProperty, "Localization.Control.RESET");
			rightGrid.Children.Add(ResetButton);
			Grid.SetRow(ResetButton, 2);


			ConfirmButton = new Button
			{
				BorderThickness = new Thickness(2),
				Margin = new Thickness(0, 1, 2, 0),
				IsEnabled = false,
			};
			ConfirmButton.Click += (s, e) =>
			{
				HackKernel hackKernel = HackKernel.Create(Process.GetProcessById(PID));
				MainWindow m = new MainWindow(hackKernel);
				Close();
				m.Show();
			};
			ConfirmButton.SetResourceReference(ContentProperty, "Localization.Control.CONFIRM");
			rightGrid.Children.Add(ConfirmButton);
			Grid.SetRow(ConfirmButton, 3);

			LoadCores();
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			PID = 0;
			CoresList.SelectedItem = null;
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			if (CoresList.SelectedItem != null)
			{
				if (PID == 0)
				{
					DraggableCross.IsEnabled = true;
					StatusItem.SetResourceReference(ContentProperty, "Localization.Control.DRAG_CROSS");
				}
				else
				{
					ConfirmButton.IsEnabled = true;
					DraggableCross.IsEnabled = false;
					CoresList.IsEnabled = false;
					StatusItem.Content = "ProcessID: " + PID;
					//StatusItem.SetResourceReference(ContentProperty, "Localization.Control.STARTUP_CLICK_CONFIRM");
					Task.Run(() =>
					{
						System.Threading.Thread.Sleep(2000);
						Application.Current.Dispatcher.Invoke(() =>
						{
							if (ConfirmButton.IsEnabled)
								StatusItem.SetResourceReference(ContentProperty, "Localization.Control.STARTUP_CLICK_CONFIRM");
						});
					});
				}
			}
			else
			{
				ConfirmButton.IsEnabled = false;
				DraggableCross.IsEnabled = false;
				CoresList.IsEnabled = true;
				StatusItem.SetResourceReference(ContentProperty, "Localization.Control.SELECT_CORE");
			}
		}

		private void LoadCores()
		{
			Cores.Clear();
			foreach (var file in Directory.EnumerateFiles(DIR_CORES, "*.dll"))
			{
				Assembly asm = Assembly.LoadFrom(file);
				TypeInfo[] ts = asm.DefinedTypes.Where(t => t.IsSubclassOf(typeof(BaseCore))).ToArray();
				if (ts.Length == 0)
					throw new HackKernelException($"Cannot find Core class. Assembly: {asm.FullName}");
				else if (ts.Length > 1)
					throw new HackKernelException($"More than 1 Core class found. Assembly: {asm.FullName}");
				BaseCore core = ts[0].GetConstructor(Type.EmptyTypes).
					Invoke(null) as BaseCore;//construct
				Cores[core.VersionSig.ToString()] = core;
			}
			CoresList.Items.Clear();
			foreach (var coreSig in Cores.Keys)
			{
				CoresList.Items.Add(coreSig);
			}
		}

		private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Application.Current.Resources.MergedDictionaries.Add(
				new ResourceDictionary() { Source = new Uri($"pack://application:,,,/Resources/Languages/{e.AddedItems[0]}.xaml") });
		}

		[STAThread]
		public static void Main()
		{
			Application app = new Application();
			app.Resources.MergedDictionaries.Add(
				new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml") });
			app.Resources.MergedDictionaries.Add(
				new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml") });
			app.Resources.MergedDictionaries.Add(
				new ResourceDictionary() { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Themes/Light.Pink.xaml") });
			app.Run(new StartupWindow());
		}

	}
}
