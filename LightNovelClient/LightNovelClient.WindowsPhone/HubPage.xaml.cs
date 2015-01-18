//using LightNovel.Common;
//using LightNovel.Service;
//using LightNovel.ViewModels;
//using Q42.WinRT.Data;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using Windows.Foundation;
//using Windows.UI.Popups;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Data;
//using Windows.UI.Xaml.Media;
//using Windows.UI.Xaml.Navigation;
//using WinRTXamlToolkit.Controls.Extensions;

//// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

//namespace LightNovel
//{

//	/// <summary>
//	/// A page that displays a grouped collection of items.
//	/// </summary>
//	public sealed partial class HubPage : Page
//	{
//		private NavigationHelper navigationHelper;
//		//private ObservableDictionary defaultViewModel = new ObservableDictionary();
//		//private Dictionary<string, object> resources = new Dictionary<string, object>();
//		private MainViewModel _viewModel = new MainViewModel();
//		private ScrollViewer _hubScrollViewer;

//		public ScrollViewer HubScrollViewer
//		{
//			get
//			{
//				if (_hubScrollViewer == null)
//					_hubScrollViewer = VisualTreeHelperExtensions.GetFirstDescendantOfType<ScrollViewer>(RootHub);
//				return _hubScrollViewer;
//			}
//		}
//		/// <summary>
//		/// Gets the NavigationHelper used to aid in navigation and process lifetime management.
//		/// </summary>
//		public NavigationHelper NavigationHelper
//		{
//			get { return this.navigationHelper; }
//		}

//		/// <summary>
//		/// Gets the DefaultViewModel. This can be changed to a strongly typed view model.
//		/// </summary>
//		//public ObservableDictionary DefaultViewModel
//		//{
//		//	get { return this.defaultViewModel; }
//		//}

//		public MainViewModel ViewModel
//		{
//			get
//			{
//				return this._viewModel;
//			}
//		}

//		public HubPage()
//		{
//			this.InitializeComponent();
//			this.navigationHelper = new NavigationHelper(this);
//			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
//			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
//			ViewModel.Settings = App.Current.Settings;
//		}

//		/// <summary>
//		/// Populates the page with content passed during navigation.  Any saved state is also
//		/// provided when recreating a page from a prior session.
//		/// </summary>
//		/// <param name="sender">
//		/// The source of the event; typically <see cref="NavigationHelper"/>
//		/// </param>
//		/// <param name="e">Event data that provides both the navigation parameter passed to
//		/// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
//		/// a dictionary of state preserved by this page during an earlier
//		/// session.  The state will be null the first time a page is visited.</param>
//		private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
//		{
//			// TODO: Create an appropriate data model for your problem domain to replace the sample data
//			//var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-4");

//			//Task<Session> LoginTask = null;
//			Task LoadingRecommandTask = null;
//			Task LoadingFavouriteTask = null;
//			Task LoadingRecentTask = null;
//			Task LoginTask = null;

//			if (!ViewModel.RecommandSection.IsLoaded && !ViewModel.RecommandSection.IsLoading)
//			{
//				LoadingRecommandTask = ViewModel.RecommandSection.LoadAsync();
//			}

//			LoadingRecentTask = ViewModel.RecentSection.LoadLocalAsync();

//			if (!App.Current.IsSignedIn)
//			{
//				LoginTask = ViewModel.TryLogInWithStoredCredentialAsync();
//			}

//			if (LoadingRecentTask != null)
//			{
//				await LoadingRecentTask;
//				if (ViewModel.RecentSection.Count > 0)
//				{
//					ViewModel.LastReadSection = ViewModel.RecentSection.FirstOrDefault();
//					ViewModel.RecentSection.RemoveAt(0);
//				}
//			}

//			if (LoginTask != null && App.IsConnectedToInternet())
//				await LoginTask;

//			ViewModel.IsSignedIn = App.Current.IsSignedIn;
//			if (ViewModel.IsSignedIn)
//			{
//				ViewModel.UserName = App.Current.User.UserName;
//			}

//			if (LightKindomHtmlClient.IsSignedIn && !ViewModel.FavoriteSection.IsLoaded && !ViewModel.FavoriteSection.IsLoading && App.IsConnectedToInternet())
//			{
//				LoadingFavouriteTask = ViewModel.FavoriteSection.LoadAsync();
//				//var his = await LightKindomHtmlClient.GetUserRecentViewedVolumesAsync();
//			}

//			if (LoadingRecommandTask != null)
//				await LoadingRecommandTask;
//			if (LoadingFavouriteTask != null)
//				await LoadingFavouriteTask;

//			if (ViewModel.LastReadSection == null)
//			{
//				var bvm = ViewModel.RecommandSection[0][0];
//				ViewModel.LastReadSection = new HistoryItemViewModel
//				{
//					Position = new NovelPositionIdentifier
//					{
//						SeriesId = bvm.Id,
//						VolumeNo = 0,
//						ChapterNo = 0,
//					},
//					SeriesTitle = bvm.Title,
//					Description = bvm.Description,
//					CoverImageUri = bvm.CoverImageUri,
//				};
//				try
//				{
//					var series = await CachedClient.GetSeriesAsync(bvm.Id);
//					var chapter = await CachedClient.GetChapterAsync(series.Volumes[0].Chapters[0].Id);
//					var imageLine = chapter.Lines.FirstOrDefault(line => line.ContentType == LineContentType.ImageContent);
//					ViewModel.LastReadSection.CoverImageUri = imageLine.Content;
//					ViewModel.LastReadSection.Description = series.Description;
//				}
//				catch (Exception)
//				{
//					Debug.WriteLine("Failed to download the Cover Image.");
//				}
//			}

//			// Recover the scroll offset
//			if (e.PageState != null)
//			{
//				var offset = (double)e.PageState["HubOffset"];
//				if (HubScrollViewer != null)
//					HubScrollViewer.ChangeView(offset, null, null, true);
//			}

//			if (HubScrollViewer != null)
//				HubScrollViewer.ViewChanged += HubScrollViewer_ViewChanged;

//			foreach (var group in ViewModel.RecommandSection)
//			{
//				foreach (var item in group)
//				{
//					try
//					{
//						await item.LoadDescriptionAsync();
//					}
//					catch (Exception exception)
//					{
//						Debug.WriteLine("Exception in loading volume description : ({0},{1}), exception : {3}", item.Title, item.Id, exception.Message);
//					}

//				}
//			}
//		}

//		void HubScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
//		{
//			ScrollViewer viewer = sender as ScrollViewer;
//			LogoImageTranslate.X = -Math.Min(viewer.HorizontalOffset, 600);
//		}

//		private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
//		{
//			e.PageState.Add("HubOffset", HubScrollViewer.HorizontalOffset);
//			//foreach (var item in resources)
//			//{
//			//	e.PageState.Add(item.Key, item.Value);
//			//}
//		}
//		/// <summary>
//		/// Invoked when a HubSection header is clicked.
//		/// </summary>
//		/// <param name="sender">The Hub that contains the HubSection whose header was clicked.</param>
//		/// <param name="e">Event data that describes how the click was initiated.</param>
//		void Hub_SectionHeaderClick(object sender, HubSectionHeaderClickEventArgs e)
//		{
//			HubSection section = e.Section;
//			//var group = section.DataContext;
//			//this.Frame.Navigate(typeof(SectionPage), ((SampleDataGroup)group).UniqueId);
//			//this.Frame.Navigate(typeof(SectionPage));
//		}

//		void FavoriteSectionItem_ItemClike(object sender, ItemClickEventArgs e)
//		{
//			FavourVolume item = (FavourVolume)e.ClickedItem;
//			this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { VolumeId = item.VolumeId }.ToString());
//		}
//		/// <summary>
//		/// Invoked when an recommend item within a section is clicked.
//		/// </summary>
//		/// <param name="sender">The GridView or ListView
//		/// displaying the item clicked.</param>
//		/// <param name="e">Event data that describes the item clicked.</param>
//		void RecommandSectionItem_ItemClick(object sender, ItemClickEventArgs e)
//		{
//			// Navigate to the appropriate destination page, configuring the new page
//			// by passing required information as a navigation parameter
//			var book = (BookCoverViewModel)e.ClickedItem;
//			if (book.ItemType == BookItemType.Volume)
//			{
//				this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { VolumeId = book.Id }.ToString());
//			}
//			else if (book.ItemType == BookItemType.Series)
//			{
//				this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { SeriesId = book.Id }.ToString());
//			}
//		}


//		private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
//		{
//			ViewModel.IsLoading = true;
//			await DataCache.ClearAll();
//			ViewModel.IsLoading = false;
//			//MessageBox.Show("Web data cache cleared.");
//		}

//		private void ClearHistoryButtonBase_Click(object sender, RoutedEventArgs e)
//		{
//			//using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
//			//{
//			//	App.HistoryList.Clear();
//			//	App.IsHistoryListChanged = true;
//			//	if (storage.FileExists("history.json"))
//			//		storage.DeleteFile("history.json");
//			//	await ViewModel.UpdateRecentViewAsync();
//			//	//(ContentPanorama.Background as ImageBrush).ImageSource = new BitmapImage(ViewModel.CoverBackgroundImageUri);
//			//}
//			//MessageBox.Show("History cleared.");
//		}

//		private void ClearBookmarkButtonBase_Click(object sender, RoutedEventArgs e)
//		{
//			//using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
//			//{
//			//	if (storage.FileExists("bookarmks.json"))
//			//		storage.DeleteFile("bookarmks.json");
//			//}
//			//MessageBox.Show("Bookmark cleared.");
//		}

//		private async void LoginButton_Click(object sender, RoutedEventArgs e)
//		{
//			await ViewModel.TryLogInWithUserInputCredentialAsync();
//		}

//		#region NavigationHelper registration

//		/// <summary>
//		/// The methods provided in this section are simply used to allow
//		/// NavigationHelper to respond to the page's navigation methods.
//		/// Page specific logic should be placed in event handlers for the  
//		/// <see cref="Common.NavigationHelper.LoadState"/>
//		/// and <see cref="Common.NavigationHelper.SaveState"/>.
//		/// The navigation parameter is available in the LoadState method 
//		/// in addition to page state preserved during an earlier session.
//		/// </summary>
//		protected override void OnNavigatedTo(NavigationEventArgs e)
//		{
//			this.navigationHelper.OnNavigatedTo(e);
//		}

//		protected override void OnNavigatedFrom(NavigationEventArgs e)
//		{
//			this.navigationHelper.OnNavigatedFrom(e);
//		}

//		#endregion

//		private void RecentItem_Click(object sender, ItemClickEventArgs e)
//		{
//			HistoryItemViewModel item = (HistoryItemViewModel)e.ClickedItem;
//			this.Frame.Navigate(typeof(ReadingPage), item.Position.ToString());
//		}

//		private void LastReadSection_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
//		{
//			HistoryItemViewModel item = (sender as FrameworkElement).DataContext as HistoryItemViewModel;
//			e.Handled = true;
//			this.Frame.Navigate(typeof(ReadingPage), item.Position.ToString());
//		}

//		//private void SearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
//		//{
//		//	this.Frame.Navigate(typeof(SearchResultsPage), args.QueryText);
//		//}

//		public static Rect GetElementRect(FrameworkElement element)
//		{
//			GeneralTransform buttonTransform = element.TransformToVisual(null);
//			Point point = buttonTransform.TransformPoint(new Point());
//			return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
//		}

//		private async void UserAccount_Click(object sender, RoutedEventArgs e)
//		{
//			var menu = new PopupMenu();
//			menu.Commands.Add(new UICommand("Log out", (command) =>
//			{
//				ViewModel.LogOut();
//				//App.CurrentState.SignOut();
//			}));
//			var chosenCommand = await menu.ShowForSelectionAsync(GetElementRect((FrameworkElement)sender));
//			//if (chosenCommand == null)
//		}
//	}
//}
