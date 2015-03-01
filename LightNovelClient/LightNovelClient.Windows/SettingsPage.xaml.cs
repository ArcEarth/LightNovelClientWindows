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

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace LightNovel
{
	public sealed partial class SettingsPage : SettingsFlyout
	{
		public ApplicationSettings ViewModel
		{
			get { return App.Current.Settings; }
		}
		/// <summary>
		/// NavigationHelper is used on each page to aid in navigation and 
		/// process lifetime management
		/// </summary>

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
				var frame = Window.Current.Content as Frame;
				if (frame != null)
				{
					var page = frame.Content as Page;
					page.RequestedTheme = (Windows.UI.Xaml.ElementTheme)(combo.SelectedIndex);
				}
			}
		}
		public SettingsPage()
		{
			this.RequestedTheme = App.Current.Settings.BackgroundTheme;

			this.InitializeComponent();

			var lan = App.Current.Settings.InterfaceLanguage ;
			var combo = InterfaceLanguageComboBox.Items.FirstOrDefault(item => ((ComboBoxItem)item).Language == lan);
			if (combo != null)
				InterfaceLanguageComboBox.SelectedItem = combo;
			else
				InterfaceLanguageComboBox.SelectedItem = LanguageAutoItem;
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void InterfaceLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = (ComboBoxItem)(InterfaceLanguageComboBox.SelectedItem);
			string language = "";
			if (item != LanguageAutoItem)
				language = item.Language;

			App.Current.Settings.InterfaceLanguage = language;
			if (language != "")
				Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;// Windows.Globalization.ApplicationLanguages.Languages[0];
		}
	}
}
