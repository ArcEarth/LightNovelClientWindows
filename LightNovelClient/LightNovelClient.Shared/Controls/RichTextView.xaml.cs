using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LightNovel.Controls
{
	public sealed partial class RichTextView : UserControl
	{
        /// <summary>
        /// Identifies the <see cref="PageWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PageWidthProperty =
			DependencyProperty.Register("PageWidth", typeof(double),
			typeof(RichTextColumns), new PropertyMetadata(double.NaN, null));

        /// <summary>
        /// Identifies the <see cref="PageHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PageHeightProperty =
			DependencyProperty.Register("PageHeight", typeof(double),
			typeof(RichTextColumns), new PropertyMetadata(double.NaN, null));
		
		public double PageWidth
		{
			get { return (double)GetValue(PageWidthProperty); }
			set
			{
				if (Math.Abs(value - (double)GetValue(PageWidthProperty)) > 0.5)
					SetValue(PageWidthProperty, value);
				//HorizontalSnapPointsChanged(this, null);
			}
		}

		public double PageHeight
		{
			get { return (double)GetValue(PageHeightProperty); }
			set
			{
				if (Math.Abs(value - (double)GetValue(PageHeightProperty)) > 0.5)
					SetValue(PageHeightProperty, value);
				//VerticalSnapPointsChanged(this, null);
			}
		}

		private void ContentScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			if (e.IsIntermediate) return;
			int page = (int)((ContentScrollViewer.HorizontalOffset + 0.1) / ContentTextBlock.Width);
			//System.Diagnostics.Debug.WriteLine("Current page : " + page);
		}
		
		public RichTextView()
		{
			this.InitializeComponent();
		}
	}
}
