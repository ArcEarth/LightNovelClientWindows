using LightNovel.Common;
using LightNovel.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace LightNovel
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public sealed partial class App : Application
	{
#if WINDOWS_PHONE_APP
		//private TransitionCollection transitions;
#else
        public ObservableCollection<ViewLifetimeControl> SecondaryViews = new ObservableCollection<ViewLifetimeControl>();
#endif

        /// <summary>
        /// Initializes the singleton instance of the <see cref="App"/> class. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
		{
			try
			{
				if (Settings.BackgroundTheme == ElementTheme.Light)
				{
					this.RequestedTheme = ApplicationTheme.Light;
				}
				else if (Settings.BackgroundTheme == ElementTheme.Dark)
				{
					this.RequestedTheme = ApplicationTheme.Dark;
				}
				var language = Settings.InterfaceLanguage;
				if (!string.IsNullOrEmpty(language))
					Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;// Windows.Globalization.ApplicationLanguages.Languages[0];
				this.InitializeComponent();
				this.Suspending += this.OnSuspending;
				this.Resuming += this.OnResuming;
				NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception.Message);
			}
		}

		public static new App Current
		{
			get
			{
				return Application.Current as App;
			}
		}

		public void ChangeTheme(ApplicationTheme background, Windows.UI.Color accentColor)
		{

		}

		void OnResuming(object sender, object e)
		{

		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used when the application is launched to open a specific file, to display
		/// search results, and so forth.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected async override void OnLaunched(LaunchActivatedEventArgs e)
		{
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				this.DebugSettings.EnableFrameRateCounter = true;
			}
			Debug.WriteLine("AppOnLaunched");
#endif

			mainDispatcher = Window.Current.Dispatcher;
            mainViewId = ApplicationView.GetForCurrentView().Id;

            Frame rootFrame = Window.Current.Content as Frame;

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (rootFrame == null)
			{
				ExtendedSplash extendedSplash ;
				if (Window.Current.Content == null)
				{
					extendedSplash = new ExtendedSplash(e.SplashScreen);
				} else
				{
					extendedSplash = Window.Current.Content as ExtendedSplash;
				}
				extendedSplash.RegisterFrameArriveDimmsion();
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = extendedSplash.RootFrame;
				// TODO: change this value to a cache size that is appropriate for your application
				rootFrame.CacheSize = 2;

				//Associate the frame with a SuspensionManager key 
				try
				{
					SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
				}
				catch (Exception)
				{
				}

				Window.Current.Content = extendedSplash;
				Window.Current.Activate();
				// Place the frame in the current Window
				// Window.Current.Content = rootFrame;
			}
			else
			{
				Window.Current.Activate();
			}

			#region Preloading
#if WINDOWS_PHONE_APP
			//Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
			//	 .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseVisible);
			var statusBar = StatusBar.GetForCurrentView();
			statusBar.BackgroundOpacity = 0;
			statusBar.ForegroundColor = (Windows.UI.Color)Resources["AppBackgroundColor"];
#endif
			await LoadHistoryDataAsync(); ;
			await LoadBookmarkDataAsync();
			await CachedClient.InitializeCachedSetAsync();
			RefreshAutoLoadPolicy();
			#endregion

			if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
			{
				// Restore the saved session state only when appropriate
				try
				{
#if WINDOWS_PHONE_APP
					rootFrame.ContentTransitions = new TransitionCollection() { new NavigationThemeTransition() { DefaultNavigationTransitionInfo = new ContinuumNavigationTransitionInfo() } };
#endif
					await SuspensionManager.RestoreAsync();
					if (rootFrame.Content != null && Window.Current.Content != rootFrame)
					{
						Window.Current.Content = rootFrame;
					}
				}
				catch (SuspensionManagerException)
				{
					// Something went wrong restoring state.
					// Assume there is no state and continue
				}
			}

			if (rootFrame.Content == null)
			{
#if WINDOWS_PHONE_APP
				// Removes the turnstile navigation for startup.
				rootFrame.ContentTransitions = null;
				rootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif
			}

			// When the navigation stack isn't restored navigate to the first page,
			// configuring the new page by passing required information as a navigation
			// parameter


			bool navResult = true;

			if (string.IsNullOrEmpty(e.Arguments))
			{
				var entity = rootFrame.BackStack.FirstOrDefault(pse => pse.SourcePageType == typeof(HubPage));
				if (rootFrame.CurrentSourcePageType != typeof(HubPage) && entity == null)
					navResult = rootFrame.Navigate(typeof(HubPage), e.Arguments);
				else
				{
					while (rootFrame.CurrentSourcePageType != typeof(HubPage) && rootFrame.CanGoBack)
						rootFrame.GoBack();
				}
			}
			else
			{
				var entity = rootFrame.BackStack.FirstOrDefault(pse => pse.SourcePageType == typeof(ReadingPage));
				if (entity == null && rootFrame.CurrentSourcePageType != typeof(ReadingPage))
					navResult = rootFrame.Navigate(typeof(ReadingPage), e.Arguments);
				else
				{
					while (rootFrame.CurrentSourcePageType != typeof(ReadingPage) && rootFrame.CanGoBack)
						rootFrame.GoBack();
					var page = rootFrame.Content as ReadingPage;
					var navigationId = NovelPositionIdentifier.Parse((string)e.Arguments);
					if (navigationId.SeriesId != page.ViewModel.SeriesId.ToString())
						navResult = rootFrame.Navigate(typeof(ReadingPage), e.Arguments);
				}
			}
			if (!navResult)
			{
				navResult = rootFrame.Navigate(typeof(HubPage), null);
			}


			Settings.UpdateSavedAppVersion();
			//App.CurrentState.SignInAutomaticlly();
			// Ensure the current window is active
			//Window.Current.Activate();
		}

#if WINDOWS_PHONE_APP
		/// <summary>
		/// Restores the content transitions after the app has launched.
		/// </summary>
		/// <param name="sender">The object where the handler is attached.</param>
		/// <param name="e">Details about the navigation event.</param>
		private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
		{
			var rootFrame = sender as Frame;
			if (rootFrame.ContentTransitions == null)
				rootFrame.ContentTransitions = new TransitionCollection() { new NavigationThemeTransition() { DefaultNavigationTransitionInfo = new ContinuumNavigationTransitionInfo() } };
			rootFrame.Navigated -= this.RootFrame_FirstNavigated;
		}
#else
		public static void ApplicationWiseCommands_CommandsRequested(Windows.UI.ApplicationSettings.SettingsPane sender, Windows.UI.ApplicationSettings.SettingsPaneCommandsRequestedEventArgs args)
		{
			if (!args.Request.ApplicationCommands.Any(c => ((string)c.Id) == "Options"))
			{
				var command = new Windows.UI.ApplicationSettings.SettingsCommand("Options", "Options", x =>
				{
					var settings = new SettingsPage();

					settings.Show();
				});
				args.Request.ApplicationCommands.Add(command);
			}

			if (!args.Request.ApplicationCommands.Any(c => ((string)c.Id) == "About"))
			{
				var command = new Windows.UI.ApplicationSettings.SettingsCommand("About", "About", x =>
				{
					var settings = new AboutSettingsFlyout();
					settings.Show();
				});
				args.Request.ApplicationCommands.Add(command);
			}
		}
#endif


		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		private async void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			await SuspensionManager.SaveAsync();
			deferral.Complete();
		}


		#region CustomizedApplicationWideData
		public static bool IsConnectedToInternet()
		{
			ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
			return (connectionProfile != null && connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
		}

		static bool _shouldAutoImage = true;
		public static bool ShouldAutoLoadImage
		{
			get
			{
				return _shouldAutoImage;
			}
		}

		public static void RefreshAutoLoadPolicy()
		{
			if (App.Settings.ImageLoadingPolicy == ImageLoadingPolicy.Automatic)
				_shouldAutoImage = true;
			else if (App.Settings.ImageLoadingPolicy == ImageLoadingPolicy.Manual)
				_shouldAutoImage = false;
			else
				_shouldAutoImage = App.NetworkState == App.AppNetworkState.Unrestricted;
		}

		static void NetworkInformation_NetworkStatusChanged(object sender)
		{
			RefreshAutoLoadPolicy();
		}

		public enum AppNetworkState
		{
				Unconnected = 0,
				Metered = 1,
				Unrestricted = 2
		};

		public static AppNetworkState NetworkState
		{
			get
			{
				ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
				if (connectionProfile == null || connectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.InternetAccess)
					return AppNetworkState.Unconnected;
				if (connectionProfile.GetConnectionCost().NetworkCostType == NetworkCostType.Unrestricted)
					return AppNetworkState.Unrestricted;
				else
					return AppNetworkState.Metered;
			}
		}

		private const string LocalRecentFilePath = "history.json";
		private const string LocalBookmarkFilePath = "bookmark.json";
		private static ApplicationSettings _settings = new ApplicationSettings();
		public static UserInfo User { get; set; }
		public static ApplicationSettings Settings { get { return _settings; } }
		public bool IsSignedIn { get { return User != null && !User.Credential.Expired; } }

        public static bool _isHistoryListChanged = true;
		public static bool IsHistoryListChanged
		{
			get { return _isHistoryListChanged; }
			set { _isHistoryListChanged = value; }
		}

		public bool IsBookmarkListChanged { get; set; }
		public static List<BookmarkInfo> RecentList { get; set; }
		public static List<BookmarkInfo> BookmarkList { get; set; }

        public static event EventHandler RecentListChanged;
        public static event EventHandler BookmarkListChanged;

        public static void NotifyRecentsChanged() {
            if (RecentListChanged != null)
                RecentListChanged(Current,null);
        }

        public static void NotifyBookmarksChanged() {
            if (BookmarkListChanged != null)
                BookmarkListChanged(Current, null);
        }
        //public List<FavourVolume> FavoriteList { get; set; }

        public Windows.Storage.StorageFolder IllustrationFolder { get; set; }

		public static async Task UpdateHistoryListAsync(BookmarkInfo bookmark)
		{
			if (RecentList == null)
				await LoadHistoryDataAsync();
            //var existed = RecentList.FirstOrDefault(item => item.Position.SeriesId == bookmark.Position.SeriesId);

            //// No Changes
            //if (bookmark.Position.VolumeNo == existed.Position.VolumeNo && bookmark.Position.ChapterNo == existed.Position.ChapterNo && bookmark.Position.LineNo == existed.Position.LineNo)
            //    return;
            RecentList.RemoveAll(item => item.Position.SeriesId == bookmark.Position.SeriesId);
			RecentList.Add(bookmark);
			IsHistoryListChanged = true;
			await SaveHistoryDataAsync();
            NotifyRecentsChanged();
        }

		public static async Task<bool> UpdateSecondaryTileAsync(BookmarkInfo bookmark)
		{
			if (SecondaryTile.Exists(bookmark.Position.SeriesId))
			{
				var tile = new SecondaryTile(bookmark.Position.SeriesId);
				string args = bookmark.Position.ToString();
				tile.Arguments = args;
				var result = await tile.UpdateAsync();
				return true;
			}
			return false;
		}


		public static BitmapTransform CreateUniformToFillTransform(Size OriginalSize, Size NewSize, HorizontalAlignment hAlign = HorizontalAlignment.Center, VerticalAlignment vAlign = VerticalAlignment.Center)
		{
			var transform = new BitmapTransform();
			double wScale = (double)NewSize.Width / (double)OriginalSize.Width;
			double hScale = (double)NewSize.Height / (double)OriginalSize.Height;
			double uScale = Math.Max(wScale, hScale);
			transform.ScaledWidth = (uint)Math.Round(uScale * OriginalSize.Width);
			transform.ScaledHeight = (uint)Math.Round(uScale * OriginalSize.Height);
			BitmapBounds bound;
			bound.X = 0;
			bound.Y = 0;
			bound.Width = (uint)NewSize.Width;
			bound.Height = (uint)NewSize.Height;
			if (wScale > hScale) // Crop in height
			{
				if (vAlign == VerticalAlignment.Bottom)
					bound.Y = transform.ScaledHeight - bound.Height;
				else if (vAlign == VerticalAlignment.Center)
					bound.Y = (transform.ScaledHeight - bound.Height) / 2;
			}
			else
			{
				if (hAlign == HorizontalAlignment.Right)
					bound.Y = transform.ScaledWidth - bound.Width;
				else if (hAlign == HorizontalAlignment.Center)
					bound.Y = (transform.ScaledWidth - bound.Width) / 2;
			}
			return transform;
		}

		struct TileLogoGroup
		{
			public Uri Square150x150Logo { get; set; }
			public Uri Square30x30Logo { get; set; }
			public Uri Square310x310Logo { get; set; }
			public Uri Square70x70Logo { get; set; }
			public Uri Wide310x150Logo { get; set; }
		}
		public static async Task<Uri> CreateTileImageAsync(Uri imageUri, string fileName = null, TileSize tileSize = TileSize.Square150x150)
		{
			//BitmapImage bitmap = new BitmapImage(imageUri);
			Size imgSize;
			if (string.IsNullOrEmpty(fileName))
			{
				fileName = imageUri.LocalPath;
				fileName = Path.GetFileName(fileName);
				string sizeSuffix = "-150";
				switch (tileSize)
				{
					default:
					case TileSize.Default:
					case TileSize.Square150x150:
						sizeSuffix = "-150";
						imgSize.Height = 150;
						imgSize.Width = 150;
						break;
					case TileSize.Square30x30:
						sizeSuffix = "-30";
						imgSize.Width = 30;
						imgSize.Height = 30;
						break;
					case TileSize.Square310x310:
						sizeSuffix = "-310";
						imgSize.Width = 310;
						imgSize.Height = 310;
						break;
					//case TileSize.Square70x70:
					//	sizeSuffix = "-70";
					//	imgSize.Width = 70;
					//	imgSize.Height = 70;
					//	break;
					case TileSize.Wide310x150:
						sizeSuffix = "-310x150";
						imgSize.Width = 310;
						imgSize.Height = 150;
						break;

				}
				fileName = Path.GetFileNameWithoutExtension(fileName) + sizeSuffix + Path.GetExtension(fileName);
			}
			var localUri = new Uri(string.Format("ms-appdata:///local/illustration/{0}", fileName));

			if (Current.IllustrationFolder == null)
				Current.IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);

			StorageFile file = null;

			try
			{
				file = await Current.IllustrationFolder.GetFileAsync(fileName);
				return localUri;
			}
			catch (FileNotFoundException)
			{
			}

			try
			{
				using (var client = new HttpClient())
				{
					using (var stream = await client.GetInputStreamAsync(imageUri))
					{
						using (var memstream = new InMemoryRandomAccessStream())
						{
							await stream.AsStreamForRead().CopyToAsync(memstream.AsStreamForWrite());
							BitmapDecoder decoder = await BitmapDecoder.CreateAsync(memstream);


							file = await Current.IllustrationFolder.CreateFileAsync(fileName);

							using (var targetStream = await file.OpenAsync(FileAccessMode.ReadWrite))
							{
								BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(targetStream, decoder);

								var transform = CreateUniformToFillTransform(new Size(decoder.PixelWidth, decoder.PixelHeight), imgSize);
								encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
								encoder.BitmapTransform.ScaledHeight = transform.ScaledHeight;
								encoder.BitmapTransform.ScaledWidth = transform.ScaledWidth;
								encoder.BitmapTransform.Bounds = transform.Bounds;
								await encoder.FlushAsync();
								//WriteableBitmap wbp = new WriteableBitmap(150,150);
								//await wbp.SetSourceAsync(memstream);
								//await wbp.SaveToFile(Current.IllustrationFolder, fileName);
								return localUri;
							}
						}
					}
				}
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception.Message);
			}
			return null;
		}
		public static async Task<Uri> CacheIllustrationAsync(Uri internetUri, string fileName = null)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				fileName = internetUri.LocalPath;
				fileName = Path.GetFileName(fileName);
			}

			if (Current.IllustrationFolder == null)
				Current.IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);

			StorageFile file = null;

			var localUri = new Uri(string.Format("ms-appdata:///local/illustration/{0}", fileName));

			try
			{
				file = await Current.IllustrationFolder.GetFileAsync(fileName);
				return localUri;
			}
			catch (FileNotFoundException)
			{
			}

			try
			{
				using (var response = await HttpWebRequest.CreateHttp(internetUri).GetResponseAsync())
				{
					using (var stream = response.GetResponseStream())
					{
						file = await Current.IllustrationFolder.CreateFileAsync(fileName);
						using (var filestream = await file.OpenStreamForWriteAsync())
						{
							await stream.CopyToAsync(filestream);
							return localUri;
						}
					}
				}
			}
			catch (Exception)
			{
			}

			return null;
		}

		private async static Task<T> GetFromLocalFolderAsAsync<T>(string filePath) where T : class
		{
			try
			{
				var file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(filePath);
				var content = await Windows.Storage.FileIO.ReadTextAsync(file);
				return JsonConvert.DeserializeObject<T>(content);
			}
			catch
			{
				return null;
			}
		}
		private async static Task<T> GetFromRoamingFolderAsAsync<T>(string filePath) where T : class
		{
			try
			{
				var file = await Windows.Storage.ApplicationData.Current.RoamingFolder.GetFileAsync(filePath);
				var content = await Windows.Storage.FileIO.ReadTextAsync(file);
				return JsonConvert.DeserializeObject<T>(content);
			}
			catch
			{
				return null;
			}
		}
		private static async Task SaveToLocalFolderAsync(object obj, string path)
		{
			var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists);
			var content = JsonConvert.SerializeObject(obj);
			await Windows.Storage.FileIO.WriteTextAsync(file, content);
		}
		private static async Task<bool> SaveToRoamingFolderAsync(object obj, string path)
		{
			try
			{
				var file = await Windows.Storage.ApplicationData.Current.RoamingFolder.CreateFileAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists);
				var content = JsonConvert.SerializeObject(obj);
				await Windows.Storage.FileIO.WriteTextAsync(file, content);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
		//public Task loadHistoryDataTask = null;
		//public Task loadBookmarkDataTask = null;
		public static async Task LoadHistoryDataAsync()
		{
			if (RecentList != null)
				return;

			try
			{
				RecentList = await GetFromRoamingFolderAsAsync<List<BookmarkInfo>>(LocalRecentFilePath);

				if (RecentList == null)
				{
					RecentList = await GetFromLocalFolderAsAsync<List<BookmarkInfo>>(LocalRecentFilePath);
				}
			}
			catch
			{
			}

			if (RecentList == null)
				RecentList = new List<BookmarkInfo>();
		}

		public static async Task SaveHistoryDataAsync()
		{
			if (RecentList.Count > 30)
				RecentList.RemoveRange(0, RecentList.Count - 30);
			if (RecentList != null)
			{
				await SaveToRoamingFolderAsync(RecentList, LocalRecentFilePath);
				IsHistoryListChanged = false;
			}
		}
		public static async Task LoadBookmarkDataAsync()
		{
			if (BookmarkList != null)
				return;
			BookmarkList = await GetFromRoamingFolderAsAsync<List<BookmarkInfo>>(LocalBookmarkFilePath);

			if (BookmarkList == null)
				BookmarkList = new List<BookmarkInfo>();

		}
		public static async Task SaveBookmarkDataAsync()
		{
			if (BookmarkList.Count > 100)
				BookmarkList.RemoveRange(0, BookmarkList.Count - 100);
			if (BookmarkList != null)
			{
				await SaveToRoamingFolderAsync(BookmarkList, LocalBookmarkFilePath);
			}
		}

		public async Task PullBookmarkFromUserFavoriteAsync(bool forectRefresh = false, bool forceSyncFromCloud = false)
		{
			if (BookmarkList == null)
				await LoadBookmarkDataAsync();
			if (User != null)
			{
				await User.SyncFavoriteListAsync(forectRefresh);
				var favList = from fav in User.FavoriteList orderby fav.VolumeId group fav by fav.SeriesTitle;
				bool Changed = false;

				if (forceSyncFromCloud)
				{
					for(int i=0; i< BookmarkList.Count; i++)
					{
						var bk = BookmarkList[i];
						if (!favList.Any(g=>g.First().SeriesTitle == bk.SeriesTitle))
						{
							BookmarkList.RemoveAt(i--);
							Changed = true;
						}
					}
				}

				foreach (var series in favList)
				{
					var vol = series.LastOrDefault();
					if (BookmarkList.Any(bk => bk.SeriesTitle == vol.SeriesTitle))
						continue;
					var item = new BookmarkInfo { SeriesTitle = vol.SeriesTitle, VolumeTitle = vol.VolumeTitle, ViewDate = vol.FavTime };
					item.Position = new NovelPositionIdentifier { /*SeriesId = volume.ParentSeriesId,*/ VolumeId = vol.VolumeId, VolumeNo = -1 };
					BookmarkList.Add(item);
					Changed = true;
				}
				if (Changed)
					await App.SaveBookmarkDataAsync();
			}
		}

		public async Task PushBookmarkToUserFavortiteAsync(bool forceSyncFromCloud = false)
		{
			//if (forceSyncFromCloud)
			//{
			//	var favList = from fav in User.FavoriteList orderby fav.VolumeId group fav by fav.SeriesTitle;
			//	List<string> 
			//	foreach (var series in favList)
			//	{
			//		var vol = series.LastOrDefault();
			//		if (BookmarkList.Any(bk => bk.SeriesTitle == vol.SeriesTitle))
			//			continue;

			//	}
			//}
			foreach (var bk in BookmarkList)
			{
				if (!User.FavoriteList.Any(fav => bk.Position.VolumeId == fav.VolumeId))
				{
					var result = await LightKindomHtmlClient.AddUserFavoriteVolume(bk.Position.VolumeId);
				}
			}
		}

		public async Task<UserInfo> SignInAsync(string userName, string password)
		{
			try
			{
				var session = await LightKindomHtmlClient.LoginAsync(userName, password);
				if (session == null)
					return null;
				App.Settings.Credential = session;
				App.Settings.SetUserNameAndPassword(userName, password);
				User = new UserInfo { UserName = userName, Password = password, Credential = session };
			}
			catch (Exception)
			{
				return null;
			}
			return User;
		}

		public async Task<bool> SignInAutomaticllyAsync(bool forecRefresh = false)
		{
			var userName = App.Settings.UserName;
			var session = App.Settings.Credential;
			var password = App.Settings.Password;
			if (String.IsNullOrEmpty(password) || String.IsNullOrEmpty(userName))
				return false;
			try
			{
				if (!forecRefresh && !session.Expired)
				{
					LightKindomHtmlClient.Credential = session;
					User = new UserInfo { UserName = userName, Password = password, Credential = session };
				}
				else
				{
					var newSession = await LightKindomHtmlClient.LoginAsync(userName, password);
					if (newSession == null)
						return false;
					App.Settings.Credential = newSession;
					User = new UserInfo { UserName = userName, Password = password, Credential = newSession };
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> SignOutAsync()
		{
			if (!IsSignedIn) return true;
			App.Settings.SetUserNameAndPassword("", "");
			App.Settings.Credential = new Session();
			var clearTask = App.User.ClearUserInfoAsync();
			await clearTask;
			App.User = null;
			return true;
		}

        private CoreDispatcher mainDispatcher;
        public CoreDispatcher MainDispatcher
        {
            get
            {
                return mainDispatcher;
            }
        }

        private int mainViewId;
        public int MainViewId
        {
            get
            {
                return mainViewId;
            }
        }
        #endregion
    }
}