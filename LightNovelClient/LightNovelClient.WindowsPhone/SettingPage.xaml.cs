using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// TODO: Connect the Search Results Page to your in-app search.
// The Search Results Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234240

namespace LightNovel
{
	/// <summary>
	/// This page displays search results when a global search is directed to this application.
	/// </summary>
	public sealed partial class SettingPage : Page
	{
		private NavigationHelper navigationHelper;

		public ApplicationSettings ViewModel
		{
			get { return App.Current.Settings; }
		}
		/// <summary>
		/// NavigationHelper is used on each page to aid in navigation and 
		/// process lifetime management
		/// </summary>
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}

		void ClearTile()
		{
			var updater = TileUpdateManager.CreateTileUpdaterForApplication();
			updater.Clear();
		}

		private void LiveTileSwitch_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			var switcher = sender as ToggleSwitch;
			if (switcher.IsOn)
			{
				App.Current.Settings.EnableLiveTile = true;
			}
			else
			{
				App.Current.Settings.EnableLiveTile = false;
				ClearTile();
			}
		}

		private void BackgroundThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var combo = sender as ComboBox;
			if (combo.SelectedIndex >= 0)
			{
				this.RequestedTheme = (Windows.UI.Xaml.ElementTheme)(combo.SelectedIndex);
			}
		}
		public SettingPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += navigationHelper_LoadState;
			this.navigationHelper.SaveState += navigationHelper_SaveState;
		}
		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="navigationParameter">The parameter value passed to
		/// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
		/// </param>
		/// <param name="pageState">A dictionary of state preserved by this page during an earlier
		/// session.  This will be null the first time a page is visited.</param>
		private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
		{
		}
		private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
		}

		#region NavigationHelper registration

		/// The methods provided in this section are simply used to allow
		/// NavigationHelper to respond to the page's navigation methods.
		/// 
		/// Page specific logic should be placed in event handlers for the  
		/// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
		/// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
		/// The navigation parameter is available in the LoadState method 
		/// in addition to page state preserved during an earlier session.

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			navigationHelper.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			navigationHelper.OnNavigatedFrom(e);
		}

		#endregion



	}
}
