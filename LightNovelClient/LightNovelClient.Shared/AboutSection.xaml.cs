using LightNovel.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LightNovel
{
    public sealed partial class AboutSection : StackPanel
    {
		public ApplicationSettings ViewModel
		{
			get { return App.Settings; }
		}

		public AboutSection()
		{
			this.RequestedTheme = App.Settings.BackgroundTheme;
			this.InitializeComponent();
		}
    }
}
