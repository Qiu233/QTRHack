using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QTRHack.UI.Controls
{
	public class DraggableCross : Image
	{
		private bool Dragginng = false;
		private static readonly ImageSource CrossImage;
		public event Action<Point> OnCrossRelease;
		static DraggableCross()
		{
			WriteableBitmap img = new WriteableBitmap(50, 50, 96, 96, PixelFormats.Bgra32, null);
			CrossImage = img;
			unsafe
			{
				img.Lock();
				IntPtr baseAddr = img.BackBuffer;

				for (int i = 0; i < 50; i++)
				{
					uint* ptrA = (uint*)(baseAddr + (img.BackBufferStride * 24) + (i * 4));
					uint* ptrB = (uint*)(baseAddr + (img.BackBufferStride * 25) + (i * 4));
					*ptrA = 0xFF000000;
					*ptrB = 0xFF000000;
				}

				for (int i = 0; i < 50; i++)
				{
					ulong* ptr = (ulong*)(baseAddr + (img.BackBufferStride * i) + (24 * 4));
					*ptr = 0xFF000000_FF000000;
				}
				img.AddDirtyRect(new Int32Rect(0, 24, 50, 2));
				img.AddDirtyRect(new Int32Rect(24, 0, 2, 50));
				img.Unlock();
			}
		}
		public DraggableCross()
		{
			Source = CrossImage;
			IsEnabledChanged += DraggableCross_IsEnabledChanged;
		}

		private void DraggableCross_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
				Opacity = 1;
			else
				Opacity = 0.05;
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			CaptureMouse();
			Dragginng = true;
			Opacity = 0.05;
		}
		protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonUp(e);
			if (Dragginng)
			{
				ReleaseMouseCapture();
				Dragginng = false;
				OnCrossRelease?.Invoke(e.GetPosition(this));
			}
		}
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			Cursor = Cursors.Cross;
		}
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			if (!Dragginng)
				Cursor = Cursors.Arrow;
		}
	}
}
