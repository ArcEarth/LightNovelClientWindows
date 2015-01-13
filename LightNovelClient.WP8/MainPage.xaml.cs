using System;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using LightNovel.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Q42.WinRT.Data;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;

namespace LightNovel
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		public MainViewModel ViewModel
		{
			get { return DataContext as MainViewModel; }
		}
		public MainPage()
		{
			InitializeComponent();

			// Set the data context of the listbox control to the sample data
			DataContext = App.MainPageViewModel;
		}

		// Load data for the ViewModel Items
		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			if (!ViewModel.IsSignedIn)
			{
				await ViewModel.TryLoginAsync();
			}

			if (ContentPanorama.SelectedItem == RecentReadingItem && !ViewModel.IsRecentDataLoaded)
			{
				await ViewModel.UpdateRecentViewAsync();
			}
			else if (ContentPanorama.SelectedItem == SeriesIndexItem && !ViewModel.IsIndexDataLoaded)
			{
				await ViewModel.LoadSeriesIndexDataAsync();
			}
			else if (ContentPanorama.SelectedItem == RecommandItem && !ViewModel.IsRecommandLoaded)
			{
				await ViewModel.LoadRecommandDataAsync();
			}
			else if (ContentPanorama.SelectedItem == FavoriteSection && !ViewModel.IsFavoriteLoaded)
			{
				await ViewModel.LoadUserFavouriateAsync();
			}
			//else if (ContentPanorama.SelectedItem == UserRecentSection && !ViewModel.IsUserRecentLoaded)
			//{
			//	await ViewModel.LoadUserRecentAsync();
			//}
		}
		//private void ChapterPreview_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		//{
		//    var grid = sender as Grid;
		//    var cpvm = grid.DataContext as ViewModels.ChapterPreviewModel;
		//    var chapterViewUri = new Uri("/ChapterViewPage.xaml?id=" + cpvm.ID, UriKind.Relative);
		//    NavigationService.Navigate(chapterViewUri);
		//}

		private void SeriesName_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var elem = sender as FrameworkElement;
			var spvm = elem.DataContext as ViewModels.SeriesPreviewModel;
			var uri = new Uri(spvm.NavigateUri, UriKind.Relative);
			NavigationService.Navigate(uri);

		}

		private async void Panorama_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Debug.WriteLine("SelectionChanged Fired." + e.AddedItems);
			var currentPanoramaItem = e.AddedItems[0] as PanoramaItem;
			if (currentPanoramaItem == RecentReadingItem)
			{
				await ViewModel.UpdateRecentViewAsync();
			}
			else if (currentPanoramaItem == SeriesIndexItem && !ViewModel.IsIndexDataLoaded)
			{
				await ViewModel.LoadSeriesIndexDataAsync();
			}
			else if (currentPanoramaItem == RecommandItem && !ViewModel.IsRecommandLoaded)
			{
				await ViewModel.LoadRecommandDataAsync();
			}
			else if (ContentPanorama.SelectedItem == FavoriteSection && !ViewModel.IsFavoriteLoaded)
			{
				await ViewModel.LoadUserFavouriateAsync();
			}
			//else if (ContentPanorama.SelectedItem == UserRecentSection && !ViewModel.IsUserRecentLoaded)
			//{
			//	await ViewModel.LoadUserRecentAsync();
			//}
		}

		private async void ClearCacheButton_OnClick(object sender, RoutedEventArgs e)
		{
			ViewModel.IsLoading = true;
			await DataCache.ClearAll();
			ViewModel.IsLoading = false;
			MessageBox.Show("Web data cache cleared.");
		}

		private async void ClearHistoryButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				App.HistoryList.Clear();
				App.IsHistoryListChanged = true;
				if (storage.FileExists("history.json"))
					storage.DeleteFile("history.json");
				await ViewModel.UpdateRecentViewAsync();
				//(ContentPanorama.Background as ImageBrush).ImageSource = new BitmapImage(ViewModel.CoverBackgroundImageUri);
			}
			MessageBox.Show("History cleared.");
		}

		private void ClearBookmarkButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				if (storage.FileExists("bookarmks.json"))
					storage.DeleteFile("bookarmks.json");
			}
			MessageBox.Show("Bookmark cleared.");
		}

		private void HistoryItem_OnTap(object sender, GestureEventArgs e)
		{
			var elem = sender as FrameworkElement;
			var hivm = elem.DataContext as ViewModels.HistoryItemViewModel;
			var uri = new Uri(hivm.NavigateUri, UriKind.Relative);
			NavigationService.Navigate(uri);
		}

		private void BookCover_OnTap(object sender, GestureEventArgs e)
		{
			var elem = sender as FrameworkElement;
			var vm = elem.DataContext as ViewModels.BookCoverViewModel;
			var uri = new Uri(vm.NavigateUri, UriKind.Relative);
			NavigationService.Navigate(uri);
		}

		private async void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			await ViewModel.TryLoginAsync();
		}
		private void LogoutButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Logout();
		}

		private void RecentItem_Click(object sender, RoutedEventArgs e)
		{
			var elem = sender as FrameworkElement;
			var vm = elem.DataContext as Service.Descriptor;
			string NavigateUri = String.Format("/SeriesViewPage.xaml?id={0}&volume={1}", "", vm.Id);// It's Volume View
			var uri = new Uri(NavigateUri, UriKind.Relative);
			NavigationService.Navigate(uri);
		}
	}
}