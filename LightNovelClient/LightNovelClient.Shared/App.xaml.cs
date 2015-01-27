using System;
using System.Collections.Generic;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using LightNovel.Common;
using LightNovel.Service;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Web.Http;
using System.Net;
using Windows.UI.StartScreen;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.UI.ViewManagement;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace LightNovel
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public sealed partial class App : Application
	{
#if WINDOWS_PHONE_APP
		private TransitionCollection transitions;
#endif

		/// <summary>
		/// Initializes the singleton instance of the <see cref="App"/> class. This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
			this.Suspending += this.OnSuspending;
			this.Resuming += this.OnResuming;
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
#endif
			//CurrentState = new ApplicationState();

			Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";

#if WINDOWS_PHONE_APP
			Windows.UI.ViewManagement.ApplicationView.GetForCurrentView()
				 .SetDesiredBoundsMode(Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
			var statusBar = StatusBar.GetForCurrentView();
			statusBar.BackgroundOpacity = 0;
#endif

			Frame rootFrame = Window.Current.Content as Frame;

			#region
			loadHistoryDataTask = App.Current.LoadHistoryDataAsync();
			#endregion

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (rootFrame == null)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();

				//Associate the frame with a SuspensionManager key                                
				SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

				// TODO: change this value to a cache size that is appropriate for your application
				rootFrame.CacheSize = 1;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					// Restore the saved session state only when appropriate
					try
					{
						await SuspensionManager.RestoreAsync();
					}
					catch (SuspensionManagerException)
					{
						// Something went wrong restoring state.
						// Assume there is no state and continue
					}
				}

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}

			if (rootFrame.Content == null)
			{
#if WINDOWS_PHONE_APP
				// Removes the turnstile navigation for startup.
				if (rootFrame.ContentTransitions != null)
				{
					this.transitions = new TransitionCollection();
					foreach (var c in rootFrame.ContentTransitions)
					{
						this.transitions.Add(c);
					}
				}

				rootFrame.ContentTransitions = null;
				rootFrame.Navigated += this.RootFrame_FirstNavigated;
#endif
			}

			// When the navigation stack isn't restored navigate to the first page,
			// configuring the new page by passing required information as a navigation
			// parameter
			if (string.IsNullOrEmpty(e.Arguments))
			{
				if (rootFrame.Content == null || rootFrame.CurrentSourcePageType != typeof(HubPage))
					if (!rootFrame.Navigate(typeof(HubPage), e.Arguments))
					{
						throw new Exception("Failed to create initial page");
					}
			}
			else
			{
				if (!rootFrame.Navigate(typeof(ReadingPage), e.Arguments))
				{
					throw new Exception("Failed to create Reading Page");
				}
			}

			Settings.UpdateSavedAppVersion();
			//App.CurrentState.SignInAutomaticlly();
			// Ensure the current window is active
			Window.Current.Activate();
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
			rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() { DefaultNavigationTransitionInfo = new SlideNavigationTransitionInfo() } };
			rootFrame.Navigated -= this.RootFrame_FirstNavigated;
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

		private const string LocalRecentFilePath = "history.json";
		private const string LocalBookmarkFilePath = "bookmark.json";
		private ApplicationSettings _settings = new ApplicationSettings();
		public UserInfo User { get; set; }
		public ApplicationSettings Settings { get { return _settings; } }
		public bool IsSignedIn { get { return User != null; } }

		public bool _isHistoryListChanged = true;
		public bool IsHistoryListChanged
		{
			get { return _isHistoryListChanged; }
			set { _isHistoryListChanged = value; }
		}
		public List<BookmarkInfo> RecentList { get; set; }
		public List<FavourVolume> FavoriteList { get; set; }

		public Windows.Storage.StorageFolder IllustrationFolder { get; set; }

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
			//
			// Summary:
			//     Gets or sets the medium secondary tile image.
			//
			// Returns:
			//     The location of the image. This can be expressed as one of these schemes:
			//     ms-appx:/// A path within the deployed app package. This path is resolved
			//     for languages and DPI plateau supported by the app. ms-appdata:///local/
			//     A file found in the per-user app storage.

			public Uri Square150x150Logo { get; set; }
			//
			// Summary:
			//     Gets or sets the square 30 x 30 secondary tile image.
			//
			// Returns:
			//     The location of the image. This can be expressed as one of these schemes:
			//     ms-appx:/// A path within the deployed app package. This path is resolved
			//     for languages and DPI plateau supported by the app. ms-appdata:///local/
			//     A file found in the per-user app storage.

			public Uri Square30x30Logo { get; set; }
			//
			// Summary:
			//     Gets or sets the large secondary tile image.
			//
			// Returns:
			//     The location of the image. This can be expressed as one of these schemes:
			//     ms-appx:/// A path within the deployed app package. This path is resolved
			//     for languages and DPI plateau supported by the app. ms-appdata:///local/
			//     A file found in the per-user app storage.

			public Uri Square310x310Logo { get; set; }
			//
			// Summary:
			//     Gets or sets the small secondary tile image.
			//
			// Returns:
			//     The location of the image. This can be expressed as one of these schemes:
			//     ms-appx:/// A path within the deployed app package. This path is resolved
			//     for languages and DPI plateau supported by the app. ms-appdata:///local/
			//     A file found in the per-user app storage.

			public Uri Square70x70Logo { get; set; }
			//
			// Summary:
			//     Gets or sets the wide secondary tile image.
			//
			// Returns:
			//     The location of the image. This can be expressed as one of these schemes:
			//     ms-appx:/// A path within the deployed app package. This path is resolved
			//     for languages and DPI plateau supported by the app. ms-appdata:///local/
			//     A file found in the per-user app storage.
			public Uri Wide310x150Logo { get; set; }
		}
		//public static async Task<TileLogoGroup> CreateTileImageGroupAsync(Uri imageUri, string fileName = null)
		//{
		//	fileName = imageUri.LocalPath;
		//	fileName = Path.GetFileName(fileName);

		//	var localUri = new Uri(string.Format("ms-appdata:///local/illustration/{0}", fileName));

		//	if (Current.IllustrationFolder == null)
		//		Current.IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);

		//	StorageFile file = null;

		//	try
		//	{
		//		using (var client = new HttpClient())
		//		{
		//			using (var stream = await client.GetInputStreamAsync(imageUri))
		//			{
		//				using (var memstream = new InMemoryRandomAccessStream())
		//				{
		//					await stream.AsStreamForRead().CopyToAsync(memstream.AsStreamForWrite());
		//					BitmapDecoder decoder = await BitmapDecoder.CreateAsync(memstream);

		//					for (int ts = 1; ts <= 5; ts++)
		//					{
		//						Size imgSize;
		//						string sizeSuffix = "-150";
		//						switch ((TileSize)ts)
		//						{
		//							default:
		//							case TileSize.Default:
		//							case TileSize.Square150x150:
		//								sizeSuffix = "-150";
		//								imgSize.Height = 150;
		//								imgSize.Width = 150;
		//								break;
		//							case TileSize.Square30x30:
		//								sizeSuffix = "-30";
		//								imgSize.Width = 30;
		//								imgSize.Height = 30;
		//								break;
		//							case TileSize.Square310x310:
		//								sizeSuffix = "-310";
		//								imgSize.Width = 310;
		//								imgSize.Height = 310;
		//								break;
		//							case TileSize.Square70x70:
		//								sizeSuffix = "-70";
		//								imgSize.Width = 70;
		//								imgSize.Height = 70;
		//								break;
		//							case TileSize.Wide310x150:
		//								sizeSuffix = "-310x150";
		//								imgSize.Width = 310;
		//								imgSize.Height = 150;
		//								break;

		//						}
		//						string tileImageName = Path.GetFileNameWithoutExtension(fileName) + sizeSuffix + Path.GetExtension(fileName);
		//						file = await Current.IllustrationFolder.CreateFileAsync(fileName,CreationCollisionOption.OpenIfExists);

		//						using (var targetStream = await file.OpenAsync(FileAccessMode.ReadWrite))
		//						{
		//							BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(targetStream, decoder);
		//							var transform = CreateUniformToFillTransform(new Size(decoder.PixelWidth, decoder.PixelHeight), imgSize, HorizontalAlignment.Left);
		//							encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
		//							encoder.BitmapTransform.ScaledHeight = transform.ScaledHeight;
		//							encoder.BitmapTransform.ScaledWidth = transform.ScaledWidth;
		//							encoder.BitmapTransform.Bounds = transform.Bounds;
		//							await encoder.FlushAsync();
		//						}
		//					}
		//				}
		//			}
		//		}
		//	}
		//	catch (Exception exception)
		//	{
		//		Debug.WriteLine(exception.Message);
		//		return null;
		//	}
		//	return localUri;
		//}
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
					case TileSize.Square70x70:
						sizeSuffix = "-70";
						imgSize.Width = 70;
						imgSize.Height = 70;
						break;
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
			} catch
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
			} catch
			{
				return null;
			}
		}
		private async Task SaveToLocalFolderAsync(object obj, string path)
		{
			var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists);
			var content = JsonConvert.SerializeObject(obj);
			await Windows.Storage.FileIO.WriteTextAsync(file, content);
		}
		private async Task SaveToRoamingFolderAsync(object obj, string path)
		{
			var file = await Windows.Storage.ApplicationData.Current.RoamingFolder.CreateFileAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists);
			var content = JsonConvert.SerializeObject(obj);
			await Windows.Storage.FileIO.WriteTextAsync(file, content);
		}
		public Task loadHistoryDataTask = null;
		public async Task LoadHistoryDataAsync()
		{
			if (RecentList != null)
				return;
			if (loadHistoryDataTask != null)
			{
				await loadHistoryDataTask;
				return;
			}

			RecentList = await GetFromRoamingFolderAsAsync<List<BookmarkInfo>>(LocalRecentFilePath);

			if (RecentList == null)
			{
				RecentList = await GetFromLocalFolderAsAsync<List<BookmarkInfo>>(LocalRecentFilePath);
			}


			if (RecentList == null)
				RecentList = new List<BookmarkInfo>();

		}

		public async Task SaveHistoryDataAsync()
		{
			if (RecentList.Count > 20)
				RecentList.RemoveRange(20, RecentList.Count - 20);
			if (RecentList != null)
			{
				await SaveToRoamingFolderAsync(RecentList, LocalRecentFilePath);
				IsHistoryListChanged = false;
			}
		}

		public async Task<UserInfo> SignInAsync(string userName, string password)
		{
			try
			{
				var session = await LightKindomHtmlClient.LoginAsync(userName, password);
				if (session == null)
					return null;
				App.Current.Settings.Credential = session;
				App.Current.Settings.SetUserNameAndPassword(userName, password);
				User = new UserInfo { UserName = userName, Password = password, Credential = session };
			}
			catch (Exception)
			{
				return null;
			}
			return User;
		}

		public async Task<bool> SignInAutomaticllyAsync()
		{
			var userName = App.Current.Settings.UserName;
			var session = App.Current.Settings.Credential;
			var password = App.Current.Settings.Password;
			if (String.IsNullOrEmpty(password) || String.IsNullOrEmpty(userName))
				return false;
			try
			{
				if (!session.Expired)
				{
					LightKindomHtmlClient.Credential = session;
					User = new UserInfo { UserName = userName, Password = password, Credential = session };
				}
				else
				{
					var newSession = await LightKindomHtmlClient.LoginAsync(userName, password);
					if (newSession == null)
						return false;
					App.Current.Settings.Credential = newSession;
					User = new UserInfo { UserName = userName, Password = password, Credential = newSession };
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public bool SignOut()
		{
			if (!IsSignedIn) return true;
			App.Current.Settings.SetUserNameAndPassword("", "");
			App.Current.Settings.Credential = new Session();
			var clearTask = App.Current.User.ClearUserInfoAsync();
			App.Current.User = null;
			return true;
		}
		#endregion
	}
}