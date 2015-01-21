using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using Q42.WinRT.Data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;
using LightNovel.Controls;
using Windows.UI.ViewManagement;

namespace LightNovel
{
	public class StringToSymbolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var name = value as string;
			if (name == "Recent" || name.Contains("最近阅读"))
				return Symbol.Clock;
			if (name == "Favorite" || name.Contains("收藏"))
				return Symbol.Favorite;
			if (name.Contains("新番") || name.Contains("动画"))
				return Symbol.Play;
			if (name.Contains("热门"))
				return Symbol.Like;
			if (name.Contains("新"))
				return Symbol.Calendar;
			return Symbol.Library;
		}

		public object ConvertBack(
			object value,
			Type targetType,
			object parameter,
			string language)
		{
			throw new NotImplementedException();
		}
	}


	/// <summary>
	/// A page that displays a grouped collection of items.
	/// </summary>
	public sealed partial class HubPage : Page
	{
		private NavigationHelper navigationHelper;
		//private ObservableDictionary defaultViewModel = new ObservableDictionary();
		//private Dictionary<string, object> resources = new Dictionary<string, object>();
		private MainViewModel _viewModel = new MainViewModel();
		private ScrollViewer _hubScrollViewer;

		public ScrollViewer HubScrollViewer
		{
			get
			{
				if (_hubScrollViewer == null)
				{
					RootHub.ApplyTemplate();
					_hubScrollViewer = VisualTreeHelperExtensions.GetFirstDescendantOfType<ScrollViewer>(RootHub);
				}
				return _hubScrollViewer;
			}
		}
		/// <summary>
		/// Gets the NavigationHelper used to aid in navigation and process lifetime management.
		/// </summary>
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}

		/// <summary>
		/// Gets the DefaultViewModel. This can be changed to a strongly typed view model.
		/// </summary>
		//public ObservableDictionary DefaultViewModel
		//{
		//	get { return this.defaultViewModel; }
		//}

		public MainViewModel ViewModel
		{
			get
			{
				return this._viewModel;
			}
		}

		public HubPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
			ViewModel.Settings = App.Current.Settings;
		}

#if  WINDOWS_APP
		void HubScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			ScrollViewer viewer = sender as ScrollViewer;
			LogoImageTranslate.X = -Math.Min(viewer.HorizontalOffset, 660);
		}
#endif
		private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
			e.PageState.Add("HubOffset", HubScrollViewer.HorizontalOffset);
		}
		/// <summary>
		/// Invoked when a HubSection header is clicked.
		/// </summary>
		/// <param name="sender">The Hub that contains the HubSection whose header was clicked.</param>
		/// <param name="e">Event data that describes how the click was initiated.</param>
		void Hub_SectionHeaderClick(object sender, HubSectionHeaderClickEventArgs e)
		{
			HubSection section = e.Section;
			//var group = section.DataContext;
			//this.Frame.Navigate(typeof(SectionPage), ((SampleDataGroup)group).UniqueId);
			//this.Frame.Navigate(typeof(SectionPage));
		}

		void FavoriteSectionItem_ItemClike(object sender, ItemClickEventArgs e)
		{
			FavourVolume item = (FavourVolume)e.ClickedItem;
			this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { VolumeId = item.VolumeId }.ToString());
		}
		/// <summary>
		/// Invoked when an recommend item within a section is clicked.
		/// </summary>
		/// <param name="sender">The GridView or ListView
		/// displaying the item clicked.</param>
		/// <param name="e">Event data that describes the item clicked.</param>
		void RecommandSectionItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			// Navigate to the appropriate destination page, configuring the new page
			// by passing required information as a navigation parameter
			var book = (BookCoverViewModel)e.ClickedItem;
			if (book.ItemType == BookItemType.Volume)
			{
				this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { VolumeId = book.Id }.ToString());
			}
			else if (book.ItemType == BookItemType.Series)
			{
				this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { SeriesId = book.Id, VolumeNo = -1, ChapterNo = -1 }.ToString());
			}
		}


		private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.IsLoading = true;
			await DataCache.ClearAll();
			ViewModel.IsLoading = false;
			//MessageBox.Show("Web data cache cleared.");
		}

		private void ClearHistoryButtonBase_Click(object sender, RoutedEventArgs e)
		{
		}

		private void ClearBookmarkButtonBase_Click(object sender, RoutedEventArgs e)
		{
			//using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
			//{
			//	if (storage.FileExists("bookarmks.json"))
			//		storage.DeleteFile("bookarmks.json");
			//}
			//MessageBox.Show("Bookmark cleared.");
		}

		private async void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.IsLoading = true;
			try
			{
				var loginTask = ViewModel.TryLogInWithUserInputCredentialAsync();
				await loginTask;
			}
			catch (Exception)
			{
				Debug.WriteLine("Failed to login");
			}

			if (!App.Current.IsSignedIn)
			{
				MessageDialog diag = new MessageDialog("请检查用户名和密码,或在检查网络连接后重试", "登陆失败");
				var dialogShow = diag.ShowAsync();
				await dialogShow;
				ViewModel.IsLoading = false;
			}
			else
			{
				ViewModel.IsLoading = false;
				SigninPopup.IsOpen = false;
				SigninPopup.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				await ViewModel.FavoriteSection.LoadAsync();
			}
		}

		#region NavigationHelper registration

		/// <summary>
		/// The methods provided in this section are simply used to allow
		/// NavigationHelper to respond to the page's navigation methods.
		/// Page specific logic should be placed in event handlers for the  
		/// <see cref="Common.NavigationHelper.LoadState"/>
		/// and <see cref="Common.NavigationHelper.SaveState"/>.
		/// The navigation parameter is available in the LoadState method 
		/// in addition to page state preserved during an earlier session.
		/// </summary>
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			this.navigationHelper.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			this.navigationHelper.OnNavigatedFrom(e);
		}

		#endregion

		private void RecentItem_Click(object sender, ItemClickEventArgs e)
		{
			HistoryItemViewModel item = (HistoryItemViewModel)e.ClickedItem;
			this.Frame.Navigate(typeof(ReadingPage), item.Position.ToString());
		}

		private void LastReadSection_Clicked(object sender, object e)
		{
			HistoryItemViewModel item = (sender as FrameworkElement).DataContext as HistoryItemViewModel;
			//e.Handled = true;
			this.Frame.Navigate(typeof(ReadingPage), item.Position.ToString());
		}

		private void SearchBox_QuerySubmitted(object sender, SearchBoxQuerySubmittedEventArgs args)
		{
			if (!String.IsNullOrEmpty(args.QueryText))
				this.Frame.Navigate(typeof(SearchResultsPage), args.QueryText);
		}

		public static Rect GetElementRect(FrameworkElement element)
		{
			GeneralTransform buttonTransform = element.TransformToVisual(null);
			Point point = buttonTransform.TransformPoint(new Point());
			return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
		}

		private async void UserAccount_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.IsSignedIn)
			{
				var menu = new PopupMenu();
				menu.Commands.Add(new UICommand("Log out", (command) =>
				{
					ViewModel.LogOut();
					ViewModel.UserName = "Tap to Sign in";
				}));
				var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
			}
			else
			{
				SigninPopup.Visibility = Windows.UI.Xaml.Visibility.Visible;
				SigninPopup.IsOpen = true;
				ViewModel.UserName = "";
			}
			//if (chosenCommand == null)
		}

		private void SigninPopupBackButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.IsLoading)
				return;
			ViewModel.UserName = "Tap to Sign in";
			ViewModel.Password = "";
			SigninPopup.IsOpen = false;
			SigninPopup.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

		}

		private void PasswordBox_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
				LoginButton_Click(this, null);
			else if (e.Key == Windows.System.VirtualKey.Escape)
				SigninPopupBackButton_Click(this, null);
		}

		private void IndexListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			// Navigate to the appropriate destination page, configuring the new page
			// by passing required information as a navigation parameter
			var ser = (Descriptor)e.ClickedItem;
			this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { SeriesId = ser.Id, VolumeNo = -1, ChapterNo = -1 }.ToString());
		}

	}
}
