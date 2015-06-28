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
using Windows.UI.Notifications;
using NotificationsExtensions.TileContent;
using Windows.UI.Xaml.Input;

namespace LightNovel
{
	public class StringToSymbolConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var name = value as string;
			if (name.Contains("Recent") || name.Contains("最近阅读"))
				return Symbol.Clock;
			if (name.Contains("Favorite") || name.Contains("收藏"))
				return Symbol.Favorite;
			if (name.Contains("新番") || name.Contains("动画"))
				return Symbol.Play;
			if (name.Contains("热门"))
				return Symbol.Like;
			if (name.Contains("新"))
				return Symbol.Calendar;
			if (name.Contains("Settings") || name.Contains("设置"))
				return Symbol.Setting;
			if (name.Contains("About") || name.Contains("关于"))
				return Symbol.Message;
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
			this.SizeChanged += HubPage_SizeChanged;
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
			ViewModel.Settings = AppGlobal.Settings;
#if WINDOWS_APP
			Windows.UI.ApplicationSettings.SettingsPane.GetForCurrentView().CommandsRequested += App.ApplicationWiseCommands_CommandsRequested;
            AppGlobal.RecentListChanged += Current_RecentListChanged;
            AppGlobal.BookmarkListChanged += Current_BookmarkListChanged;
#endif

		}

		private void SyncViewWithOrientation()
		{ 
			var appView = ApplicationView.GetForCurrentView();
#if WINDOWS_PHONE_APP
			appView.SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);

			if (appView.Orientation == ApplicationViewOrientation.Landscape)
			{
				BottomAppBar.Closed += BottomAppBar_IsOpenChanged;
				BottomAppBar.Opened += BottomAppBar_IsOpenChanged;
				BottomAppBar.Opacity = 0;
				BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				AppBarHint.Visibility = Windows.UI.Xaml.Visibility.Visible;
				LastReadSection.Margin = new Thickness(0,-60,0,0);
				var logo = VisualTreeHelperExtensions.GetDescendantsOfType<Canvas>(RootHub).FirstOrDefault(elem => elem.Name == "Logo");
				if (logo != null)
				{
					(logo.RenderTransform as TranslateTransform).X = LastReadSection.ActualWidth;
				}
			}
			else
			{
				BottomAppBar.Closed -= BottomAppBar_IsOpenChanged;
				BottomAppBar.Opened -= BottomAppBar_IsOpenChanged;
				BottomAppBar.Opacity = 1;
				BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
				AppBarHint.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				LastReadSection.Margin = new Thickness(0, 0, 0, 0);
				var logo = VisualTreeHelperExtensions.GetDescendantsOfType<Canvas>(RootHub).FirstOrDefault(elem => elem.Name == "Logo");
				if (logo != null)
				{
					(logo.RenderTransform as TranslateTransform).X = 0;
				}
			}
#elif WINDOWS_APP
            if (appView.Orientation == ApplicationViewOrientation.Landscape)
            {
                LastReadSection.Margin = new Thickness(0, -79, 0, 0);
                LastReadSection.Width = (this.ActualHeight -80) * 0.6;
                LogoImage.Margin = new Thickness(this.ActualHeight * 0.6 + 40, 0, 0, 0);
                LogoShiftingFactor = 1.0;
                LastReadSection.Width = 600;
            }
            else
            {
                LastReadSection.Margin = new Thickness(0, 0, 0, 0);
                LastReadSection.Width = (this.ActualHeight - 80) * 0.6;
                LogoImage.Margin = new Thickness(20, 0,0,0);
                LogoShiftingFactor = 0.1;
            }
#endif
        }

		void BottomAppBar_IsOpenChanged(object sender, object e)
		{
			var appBar = sender as AppBar;
			if (appBar.IsOpen)
			{
				BottomAppBar.Opacity = 1;
				BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
			else {
				BottomAppBar.Opacity = 0;
				BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			}
		}

		void HubPage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SyncViewWithOrientation();
		}

		void UpdateTile()
		{
			try
			{
				var imgs = ViewModel.RecommandSection.First(g => g.Key.Contains("更新")).Take(6).ToArray();

				var tileContent = TileContentFactory.CreateTileSquare310x310ImageCollectionAndText01();
				tileContent.AddImageQuery = true;

				tileContent.ImageMain.Src = imgs[0].CoverImageUri;
				tileContent.ImageMain.Alt = imgs[0].Title;
				tileContent.ImageSmall1.Src = imgs[1].CoverImageUri;
				tileContent.ImageSmall1.Alt = imgs[1].Title;
				tileContent.ImageSmall2.Src = imgs[2].CoverImageUri;
				tileContent.ImageSmall2.Alt = imgs[2].Title;
				tileContent.ImageSmall3.Src = imgs[3].CoverImageUri;
				tileContent.ImageSmall3.Alt = imgs[3].Title;
				tileContent.ImageSmall4.Src = imgs[4].CoverImageUri;
				tileContent.ImageSmall4.Alt = imgs[4].Title;
				tileContent.TextCaptionWrap.Text = imgs[0].Description;
				// Create a notification for the Wide310x150 tile using one of the available templates for the size.
				//var wide310x150Content = TileContentFactory.CreateTileWide310x150ImageAndText01();
				//wide310x150Content.TextCaptionWrap.Text = "This tile notification uses web images";
				//wide310x150Content.Image.Src = ImgUri;
				//wide310x150Content.Image.Alt = "Web image";
				var wide310x150Content = TileContentFactory.CreateTileWide310x150PeekImageCollection05();
				wide310x150Content.AddImageQuery = true;
				//wide310x150Content.Lang = "zh-Hans";
				wide310x150Content.ImageMain.Src = imgs[0].CoverImageUri;
				wide310x150Content.ImageMain.Alt = imgs[0].Title;
				wide310x150Content.ImageSecondary.Src = imgs[0].CoverImageUri;
				wide310x150Content.ImageSecondary.Alt = imgs[0].Title;
				wide310x150Content.ImageSmallColumn1Row1.Src = imgs[1].CoverImageUri;
				wide310x150Content.ImageSmallColumn1Row1.Alt = imgs[1].Title;
				wide310x150Content.ImageSmallColumn1Row2.Src = imgs[2].CoverImageUri;
				wide310x150Content.ImageSmallColumn1Row2.Alt = imgs[2].Title;
				wide310x150Content.ImageSmallColumn2Row1.Src = imgs[3].CoverImageUri;
				wide310x150Content.ImageSmallColumn2Row1.Alt = imgs[3].Title;
				wide310x150Content.ImageSmallColumn2Row2.Src = imgs[4].CoverImageUri;
				wide310x150Content.ImageSmallColumn2Row2.Alt = imgs[4].Title;
				wide310x150Content.TextHeading.Text = imgs[0].Title;
				wide310x150Content.TextBodyWrap.Text = imgs[0].Description;

				// Create a notification for the Square150x150 tile using one of the available templates for the size.
				var square150x150Content = TileContentFactory.CreateTileSquare150x150PeekImageAndText02();
				//square150x150Content.Lang = "zh-Hans";
				square150x150Content.Image.Src = imgs[0].CoverImageUri;
				square150x150Content.Image.Alt = imgs[0].Title;
				square150x150Content.TextHeading.Text = imgs[0].Title;
				square150x150Content.TextBodyWrap.Text = imgs[0].Description;
				//var square150x150Content = TileContentFactory.CreateTileSquare150x150Image();
				//square150x150Content.Image.Src = ImgUri;
				//square150x150Content.Image.Alt = "Web image";

				var square71x71Content = TileContentFactory.CreateTileSquare71x71Image();
				square71x71Content.Image.Src = imgs[0].CoverImageUri; ;
				square71x71Content.Image.Alt = imgs[0].Title;

				// Attached the Square71x71 template to the Square150x150 template.
				square150x150Content.Square71x71Content = square71x71Content;

				// Attach the Square150x150 template to the Wide310x150 template.
				wide310x150Content.Square150x150Content = square150x150Content;

				// Attach the Wide310x150 template to the Square310x310 template.
				tileContent.Wide310x150Content = wide310x150Content;

				// Send the notification to the application’s tile.
				TileUpdateManager.CreateTileUpdaterForApplication().Update(tileContent.CreateNotification());
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception.Message);
			}
		}
		void ClearTile()
		{
			var updater = TileUpdateManager.CreateTileUpdaterForApplication();
			updater.Clear();
		}

		private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
			e.PageState.Add("HubOffset", HubScrollViewer.HorizontalOffset);
#if WINDOWS_UAP
            e.PageState.Add("IsSigninPopupOpen", false); //!Hack!!!
#else
            e.PageState.Add("IsSigninPopupOpen", SigninPopup.IsOpen);
#endif
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
		async void RecommandSectionItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			// Navigate to the appropriate destination page, configuring the new page
			// by passing required information as a navigation parameter
			var book = (BookCoverViewModel)e.ClickedItem;
			if (book.ItemType == BookItemType.Volume)
			{
				await NavigateToReadingPageAsync(book.Title, new NovelPositionIdentifier { VolumeId = book.Id });
				//this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { VolumeId = book.Id }.ToString());
			}
			else if (book.ItemType == BookItemType.Series)
			{
				await NavigateToReadingPageAsync(book.Title, new NovelPositionIdentifier { SeriesId = book.Id, VolumeNo = -1, ChapterNo = -1 });
				//this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { SeriesId = book.Id, VolumeNo = -1, ChapterNo = -1 }.ToString());
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
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			ViewModel.IsLoading = true;
			bool isAuthed = false;
			try
			{
				isAuthed = await LightKindomHtmlClient.ValidateLoginAuth();
			}
			catch (Exception)
			{
			}

			if (!isAuthed)
			{
				Frame.Navigate(typeof(AuthPage));
                return;
			}

			try
			{
				var loginTask = ViewModel.TryLogInWithUserInputCredentialAsync();
				await loginTask;
			}
			catch (Exception)
			{
				Debug.WriteLine("Failed to login");
                AppGlobal.User = null;
			}

			if (!AppGlobal.IsSignedIn)
			{
				MessageDialog diag = new MessageDialog(resourceLoader.GetString("LoginFailedMessageDialogDetail"), resourceLoader.GetString("LoginFailedMessageDialogTitle"));
				var dialogShow = diag.ShowAsync();
				await dialogShow;
				ViewModel.IsLoading = false;
			}
			else
			{
				ViewModel.IsLoading = false;
#if WINDOWS_UAP
                SigninPopup.Hide();
#else
                SigninPopup.IsOpen = false;
				SigninPopup.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
#endif
                await ViewModel.FavoriteSection.LoadAsync(true);
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

		async Task NavigateToReadingPageAsync(string seriesTitle, NovelPositionIdentifier nav, bool newWindows = false)
		{
#if WINDOWS_APP || WINDOWS_UAP
			var view = AppGlobal.SecondaryViews.FirstOrDefault(v => v.Title == seriesTitle);
			if (view == null && !newWindows)
				this.Frame.Navigate(typeof(ReadingPage), nav.ToString());
			else
				try
				{
					if (view == null)
					{
						view = await ReadingPage.CreateInNewViewAsync(seriesTitle,nav);
					}

					// Prevent the view from closing while
					// switching to it
					view.StartViewInUse();

					// Show the previously created secondary view, using the size
					// preferences the user specified. In your app, you should
					// choose a size that's best for your scenario and code it,
					// instead of requiring the user to decide.
					var viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
						view.Id,
						ViewSizePreference.Default,
						ApplicationView.GetForCurrentView().Id,
						ViewSizePreference.Default);

					if (!viewShown)
					{
						this.Frame.Navigate(typeof(ReadingPage), nav.ToString());
					}

					// Signal that switching has completed and let the view close
					view.StopViewInUse();
				}
				catch (InvalidOperationException)
				{
					Debug.WriteLine("Some thing wrong");
				}
#else
            this.Frame.Navigate(typeof(ReadingPage), nav.ToString());
#endif
		}

		private async void RecentItem_Click(object sender, ItemClickEventArgs e)
		{
			HistoryItemViewModel item = (HistoryItemViewModel)e.ClickedItem;
			await NavigateToReadingPageAsync(item.SeriesTitle, item.Position);
			//this.Frame.Navigate(typeof(ReadingPage), item.Position.ToString());
		}

		private async void LastReadSection_Clicked(object sender, object e)
		{
			HistoryItemViewModel item = (sender as FrameworkElement).DataContext as HistoryItemViewModel;
			//e.Handled = true;
			//this.Frame.Navigate(typeof(ReadingPage), item.Position.ToString());
			await NavigateToReadingPageAsync(item.SeriesTitle, item.Position);
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
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			if (ViewModel.IsSignedIn)
			{
				var menu = new PopupMenu();
				menu.Commands.Add(new UICommand(ViewModel.UserName));
				menu.Commands.Add(new UICommandSeparator());
				var syncLabel = resourceLoader.GetString("RefreshFavortite_Merge_Label");
				menu.Commands.Add(new UICommand(syncLabel, async (command) =>
				{
					ViewModel.IsLoading = true;
					await ViewModel.FavoriteSection.LoadAsync(true);
					ViewModel.IsLoading = false;
				}));

				var pullLabel = resourceLoader.GetString("RefreshFavortite_Pull_Label");
				menu.Commands.Add(new UICommand(pullLabel, async (command) =>
				{
					ViewModel.IsLoading = true;
					await ViewModel.FavoriteSection.LoadAsync(true,9,true);
					ViewModel.IsLoading = false;
				}));

				menu.Commands.Add(new UICommandSeparator());

				var logoutLabel = resourceLoader.GetString("LogoutLabel");
				menu.Commands.Add(new UICommand(logoutLabel, async (command) =>
				{
					ViewModel.UserName = resourceLoader.GetString("LogoutIndicator");
					await ViewModel.LogOutAsync();
					ViewModel.UserName = resourceLoader.GetString("LoginLabel");
				}));

				var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
			}
			else
			{
                if (ViewModel.UserName == resourceLoader.GetString("LoginLabel"))
                    ViewModel.UserName = "";
#if WINDOWS_UAP
                SigninPopup.ShowAt(AccountButton);
#else
				SigninPopup.Visibility = Windows.UI.Xaml.Visibility.Visible;
				SigninPopup.IsOpen = true;
#endif
			}
			//if (chosenCommand == null)
		}

		private void SigninPopupBackButton_Click(object sender, RoutedEventArgs e)
		{
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			if (ViewModel.IsLoading)
				return;
			ViewModel.UserName = resourceLoader.GetString("LoginLabel");
			ViewModel.Password = "";

#if WINDOWS_UAP
            SigninPopup.Hide();
#else
			SigninPopup.IsOpen = false;
			SigninPopup.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
#endif

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

		private void LiveTileSwitch_Toggled(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			var switcher = sender as ToggleSwitch;
			if (switcher.IsOn)
			{
                AppGlobal.Settings.EnableLiveTile = true;
				UpdateTile();
			}
			else
			{
                AppGlobal.Settings.EnableLiveTile = false;
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

		private async void RecentItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
		{
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			var hvm = (sender as FrameworkElement).DataContext as HistoryItemViewModel;
			var menu = new PopupMenu();

#if WINDOWS_APP
			var newWindowLabel = resourceLoader.GetString("OpenInNewWindowLabel");
			menu.Commands.Add(new UICommand(newWindowLabel, async (command) => {
				await NavigateToReadingPageAsync(hvm.SeriesTitle, hvm.Position, true);
			}));
#endif

			var label = resourceLoader.GetString("DeleteRecentLabel");
			menu.Commands.Add(new UICommand(label, async (command) =>
			{
				await RemoveRecentItem(hvm);
			}));

			try
			{
				var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
			}
			catch (Exception)
			{
			}
		}

		private async Task RemoveRecentItem(HistoryItemViewModel hvm)
		{
			ViewModel.IsLoading = true;
			await CachedClient.ClearSerialCache(hvm.Position.SeriesId);
			ViewModel.RecentSection.Remove(hvm);
			var recentItem = AppGlobal.RecentList.FirstOrDefault(it => it.Position.SeriesId == hvm.Position.SeriesId);
			if (recentItem != null)
			{
                AppGlobal.RecentList.Remove(recentItem);
				await AppGlobal.SaveHistoryDataAsync();
			}
			ViewModel.IsLoading = false;
		}

		private async void BookmarkItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
		{
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			var hvm = (sender as FrameworkElement).DataContext as HistoryItemViewModel;
			var menu = new PopupMenu();
			var label = resourceLoader.GetString("DeleteBookmarkLabel");

#if WINDOWS_APP
			var newWindowLabel = resourceLoader.GetString("OpenInNewWindowLabel");
			menu.Commands.Add(new UICommand(newWindowLabel, async (command) => {
				await NavigateToReadingPageAsync(hvm.SeriesTitle, hvm.Position, true);
			}));
#endif

			menu.Commands.Add(new UICommand(label, async (command) =>
			{
				await RemoveBookmarkFromFavorite(hvm);
			}));

			try
			{
				var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
			}
			catch (Exception)
			{
			}
		}

		private async Task RemoveBookmarkFromFavorite(HistoryItemViewModel hvm)
		{
			ViewModel.IsLoading = true;

			ViewModel.FavoriteSection.Remove(hvm);

			try
			{
				var idx = AppGlobal.BookmarkList.FindIndex(bk => bk.SeriesTitle == hvm.SeriesTitle);
				if (idx >= 0)
				{
                    AppGlobal.BookmarkList.RemoveAt(idx);
					await AppGlobal.SaveBookmarkDataAsync();
				}

				if (AppGlobal.IsSignedIn)
				{
					var favDeSer = (from fav in AppGlobal.User.FavoriteList where fav.SeriesTitle == hvm.SeriesTitle select fav.FavId).ToArray();
					if (favDeSer.Any(id => id == null))
					{
						await AppGlobal.User.SyncFavoriteListAsync(true);
						(from fav in AppGlobal.User.FavoriteList where fav.SeriesTitle == hvm.SeriesTitle select fav.FavId).ToArray();
					}

					await AppGlobal.User.RemoveUserFavriteAsync(favDeSer);
				}
			}
			catch (Exception)
			{
				Debug.WriteLine("Exception happens when deleting favorite");
			}

			//ViewModel.FavoriteSection.NotifyPropertyChanged("IsEmpty");
			ViewModel.IsLoading = false;
		}

		private void RecentItem_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
		{
			e.Handled = true;
			if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
			RecentItem_RightTapped(sender, null);
		}
		private void BookmarkItem_Holding(object sender, Windows.UI.Xaml.Input.HoldingRoutedEventArgs e)
		{
			e.Handled = true;
			if (e.HoldingState != Windows.UI.Input.HoldingState.Started) return;
			BookmarkItem_RightTapped(sender, null);
		}

		private async void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.IsLoading)
			{

			}
			else
			{
				ViewModel.IsLoading = true;
				await ViewModel.RecommandSection.LoadAsync(true, 20);
				ViewModel.IsLoading = false;
			}
		}

		private void AppBarHint_Click(object sender, RoutedEventArgs e)
		{
			if (!this.BottomAppBar.IsOpen)
			{
				this.BottomAppBar.IsOpen = true;
			}
			else
			{
				this.BottomAppBar.IsOpen = false;
			}
		}

		private void AppBarHintButton_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			e.Handled = true;
			if (e.Cumulative.Translation.Y < -25)
			{
				e.Complete();
				AppBarHint_Click(sender, null);
			}
		}
	}
}
