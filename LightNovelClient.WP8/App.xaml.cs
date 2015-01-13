using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Diagnostics;
using System.Resources;
using System.Runtime.CompilerServices;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Navigation;
using Windows.UI.Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using LightNovel.Resources;
using LightNovel.ViewModels;
using Newtonsoft.Json;
using Q42.WinRT.Data;
using LightNovel.Service;
using System.IO;
using Binding = System.Windows.Data.Binding;

namespace LightNovel
{
	public class ApplicationSettings : INotifyPropertyChanged
	{
		readonly IsolatedStorageSettings _appSettings;

		public ApplicationSettings()
		{
			_appSettings = IsolatedStorageSettings.ApplicationSettings;
			if (!_appSettings.Contains(EnableCommentsKey))
				_appSettings.Add(EnableCommentsKey,true);
			if (!_appSettings.Contains(UserNameKey))
				_appSettings.Add(UserNameKey, "");
			if (!_appSettings.Contains(PasswordKey))
				_appSettings.Add(PasswordKey, "");
			if (!_appSettings.Contains(CredentialKey))
				_appSettings.Add(CredentialKey, "");
		}



		void Save()
		{
			var settings = IsolatedStorageSettings.ApplicationSettings;
			settings.Save();
		}

		private const string EnableCommentsKey = "EnableComments";

		public bool EnableComments
		{
			get
			{
				return (bool)_appSettings[EnableCommentsKey];
			}
			set
			{
				_appSettings[EnableCommentsKey] = value;
				NotifyPropertyChanged();
				_appSettings.Save();
			}
		}

		private const string UserNameKey = "UserName";
		public string UserName
		{
			get
			{
				return (string)_appSettings[UserNameKey];
			}
			set
			{
				_appSettings[UserNameKey] = value;
				NotifyPropertyChanged();
				_appSettings.Save();
			}
		}
		private const string PasswordKey = "Password";
		public string Password
		{
			get
			{
				return (string)_appSettings[PasswordKey];
			}
			set
			{
				_appSettings[PasswordKey] = value;
				NotifyPropertyChanged();
				_appSettings.Save();
			}
		}

		private string CredentialKey = "CredentialCookie";

		public Session Credential
		{
			get
			{
				return JsonConvert.DeserializeObject<Session>((string)_appSettings[CredentialKey]);
			}
			set
			{
				_appSettings[CredentialKey] = JsonConvert.SerializeObject(value);
				NotifyPropertyChanged();
				_appSettings.Save();
			}
		}


		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public partial class App : Application
	{
		private static MainViewModel _mainViewModel = null;
		private static SeriesViewModel _seriesViewModel = null;
		private static ChapterViewModel _chapterViewModel = null;
		//private static MainViewModel _mainViewModel = null;

		private const string HistoryFilePath = "history.json";
		private const string BookmarkFilePath = "bookmark.json";

		public static bool IsHistoryListChanged = true;
		public static List<BookmarkInfo> HistoryList { get; set; }
		public static List<BookmarkInfo> BookmarkList { get; set; }
		public static ApplicationSettings Settings
		{
			get
			{
				if (_applicationSettings == null)
					_applicationSettings = new ApplicationSettings();
				return _applicationSettings;
			}
		}

		public static Series CurrentSeries { get; set; }
		public static Volume CurrentVolume { get; set; }
		public static Chapter CurrentChapter { get; set; }

		public static SeriesViewModel SeriesPageViewModel
		{
			get
			{
				// Delay creation of the view model until necessary
				if (_seriesViewModel == null)
					_seriesViewModel = new SeriesViewModel();

				return _seriesViewModel;
			}
		}
		public static ChapterViewModel ChapterPageViewModel
		{
			get
			{
				// Delay creation of the view model until necessary
				if (_chapterViewModel == null)
					_chapterViewModel = new ChapterViewModel();

				return _chapterViewModel;
			}
		}
		/// <summary>
		/// A static ViewModel used by the views to bind against.
		/// </summary>
		/// <returns>The MainViewModel object.</returns>
		public static MainViewModel MainPageViewModel
		{
			get
			{
				// Delay creation of the view model until necessary
				if (_mainViewModel == null)
					_mainViewModel = new MainViewModel(Settings);

				return _mainViewModel;
			}
		}

		//static public IsolatedStorageFile StorageFile
		//{
		//    get
		//    {
		//        if (_storageFile == null)
		//            _storageFile = IsolatedStorageFile.GetUserStoreForApplication();
		//        return _storageFile;
		//    }
		//}
		//static public IsolatedStorageSettings Settings
		//{
		//    get
		//    {
		//        if (_storageSettings == null)
		//            _storageSettings = IsolatedStorageSettings.ApplicationSettings;
		//        return _storageSettings;
		//    }
		//}

		/// <summary>
		/// Provides easy access to the root frame of the Phone Application.
		/// </summary>
		/// <returns>The root frame of the Phone Application.</returns>
		public static PhoneApplicationFrame RootFrame { get; private set; }

		/// <summary>
		/// Constructor for the Application object.
		/// </summary>
		public App()
		{
			// Global handler for uncaught exceptions.
			UnhandledException += Application_UnhandledException;

			// Standard XAML initialization
			InitializeComponent();

			// Phone-specific initialization
			InitializePhoneApplication();

			// Language display initialization
			InitializeLanguage();

			// Show graphics profiling information while debugging.
			if (Debugger.IsAttached)
			{
				// Display the current frame rate counters.
				Application.Current.Host.Settings.EnableFrameRateCounter = true;

				// Show the areas of the app that are being redrawn in each frame.
				//Application.Current.Host.Settings.EnableRedrawRegions = true;

				// Enable non-production analysis visualization mode,
				// which shows areas of a page that are handed off to GPU with a colored overlay.
				//Application.Current.Host.Settings.EnableCacheVisualization = true;

				// Prevent the screen from turning off while under the debugger by disabling
				// the application's idle detection.
				// Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
				// and consume battery power when the user is not using the phone.
				PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
			}

			App.ChapterPageViewModel.PropertyChanged += ChapterPageViewModel_PropertyChanged;
			App.SeriesPageViewModel.PropertyChanged += SeriesPageViewModel_PropertyChanged;
			//App.MainPageViewModel.PropertyChanged += MainPageViewModel_PropertyChanged;
			//var myBinding = new Binding("EnableComments");
			//myBinding.Source = this;
			//myBinding.Mode = BindingMode.TwoWay;

		}

		void SeriesPageViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var svm = sender as SeriesViewModel;
			if (svm != null && e.PropertyName == "DataContext")
			{
				CurrentSeries = svm.DataContext;
				//CurrentVolume = null;
				//CurrentChapter = null;
			}
		}

		async void ChapterPageViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var cvm = sender as ChapterViewModel;
			if (cvm != null && e.PropertyName == "DataContext")
			{
				CurrentChapter = cvm.DataContext;
				var id = CurrentChapter.ParentVolumeId;
				if (CurrentSeries == null || CurrentSeries.Id != CurrentChapter.ParentSeriesId)
				{
					try
					{
						CurrentSeries = await CachedClient.GetSeriesAsync(cvm.ParentSeriesId);
					}
					catch (Exception exception)
					{
						//MessageBox.Show("Exception happend when loading current novel Series.");
						Debug.WriteLine("Failed to retrive series data when loading a new chapter.");
					}
				}
				if (CurrentSeries == null)
					return;
				var volume = CurrentSeries.Volumes.FirstOrDefault(vol => vol.Id == id);
				if (volume != null)
					CurrentVolume = volume;
				else
				{
					throw new Exception("Current Chapter goes beyoung Current Series.");
				}
			}
		}

		// Code to execute when the application is launching (eg, from Start)
		// This code will not execute when the application is reactivated
		private void Application_Launching(object sender, LaunchingEventArgs e)
		{
			//await LoadHistoryDataAsync();
			//if (App.BookmarkList == null)
			//{
			//    App.BookmarkList = await LightKindomHtmlClient.GetBookmarkInfoAsync();
			//    if (App.BookmarkList == null)
			//        App.BookmarkList = new List<BookmarkInfo>();
			//}
		}

		private static Task<T> GetFromIsolatedStorageAsAsync<T>(string filePath) where T : class
		{
			//  var 
			return Task.Run(() =>
			{
				using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
				{
					if (!storage.FileExists(filePath))
					{
						return null;
					}
					var stream = storage.OpenFile(filePath, FileMode.Open, FileAccess.Read);
					try
					{
						//var reader = new System.Xml.Serialization.XmlSerializer(typeof(T));
						//var obj = reader.Deserialize(stream) as T;
						var serializer = new JsonSerializer();
						var obj = serializer.Deserialize(new StreamReader(stream), typeof(T)) as T;
						//stream.Close();
						return obj;
					}
					catch (Exception)
					{
						var reader = new StreamReader(stream);
						Debug.WriteLine("Exception in parsing storage history , content in history.xml : \n" +
										reader.ReadToEnd());
						//stream.Close();
						return null;
					}
				}
			});
		}

		private static Task SaveToIsolatedStorageAsync(object obj, string path)
		{
			if (obj == null)
				return null;
			return Task.Run(() =>
			{
				using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
				using (var stream = storage.OpenFile(path, FileMode.OpenOrCreate, FileAccess.Write))
				using (var writer = new StreamWriter(stream))
				{
					var serializer = new JsonSerializer();
					serializer.Serialize(writer, obj);
				}
			});
		}
		public static async Task LoadHistoryDataAsync()
		{
			if (App.HistoryList == null)
			{
				App.HistoryList = await GetHistoryListAsync();
				if (HistoryList == null)
					App.HistoryList = new List<BookmarkInfo>();
			}
		}

		// Code to execute when the application is activated (brought to foreground)
		// This code will not execute when the application is first launched
		private async void Application_Activated(object sender, ActivatedEventArgs e)
		{
			// Ensure that application state is restored appropriately
			if (App.HistoryList == null)
			{
				App.HistoryList = await GetHistoryListAsync();
				if (HistoryList == null)
					App.HistoryList = new List<BookmarkInfo>();
			}
			if (App.BookmarkList == null)
			{
				App.BookmarkList = await GetBookmarkInfoAsync();
				if (App.BookmarkList == null)
					App.BookmarkList = new List<BookmarkInfo>();
			}
		}

		static private Task<List<BookmarkInfo>> GetBookmarkInfoAsync()
		{
			return GetFromIsolatedStorageAsAsync<List<BookmarkInfo>>(BookmarkFilePath);
		}
		static public Task SaveBookmarkInfoAsync()
		{
			if (BookmarkList == null)
				return null;
			return SaveToIsolatedStorageAsync(BookmarkList, BookmarkFilePath);
		}
		static public Task SaveHistoryListAsync()
		{
			if (HistoryList == null)
				return null;
			App.IsHistoryListChanged = true;
			return SaveToIsolatedStorageAsync(HistoryList, HistoryFilePath);
		}

		static private Task<List<BookmarkInfo>> GetHistoryListAsync()
		{
			return GetFromIsolatedStorageAsAsync<List<BookmarkInfo>>(HistoryFilePath);
		}

		// Code to execute when the application is deactivated (sent to background)
		// This code will not execute when the application is closing
		private void Application_Deactivated(object sender, DeactivatedEventArgs e)
		{
			// Ensure that required application state is persisted here.
			//if (App.HistoryList != null)
			//    await LightKindomHtmlClient.SetHistoryListAsync(App.HistoryList);
			//if (App.BookmarkList != null)
			//    await LightKindomHtmlClient.SetBookmarkListAsync(App.BookmarkList);
		}

		// Code to execute when the application is closing (eg, user hit Back)
		// This code will not execute when the application is deactivated
		private void Application_Closing(object sender, ClosingEventArgs e)
		{
			//if (App.HistoryList != null)
			//    await LightKindomHtmlClient.SetHistoryListAsync(App.HistoryList);
			//if (App.BookmarkList != null)
			//    await LightKindomHtmlClient.SetBookmarkListAsync(App.BookmarkList);
		}

		// Code to execute if a navigation fails
		private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			if (Debugger.IsAttached)
			{
				// A navigation has failed; break into the debugger
				Debugger.Break();
			}
		}

		// Code to execute on Unhandled Exceptions
		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
		{
			if (Debugger.IsAttached)
			{
				// An unhandled exception has occurred; break into the debugger
				Debugger.Break();
			}
		}

		#region Phone application initialization

		// Avoid double-initialization
		private bool phoneApplicationInitialized = false;
		private static ApplicationSettings _applicationSettings;
		//static IsolatedStorageFile _storageFile;
		//static IsolatedStorageSettings _storageSettings;

		// Do not add any additional code to this method
		private void InitializePhoneApplication()
		{
			if (phoneApplicationInitialized)
				return;

			// Create the frame but don't set it as RootVisual yet; this allows the splash
			// screen to remain active until the application is ready to render.
			RootFrame = new TransitionFrame();
			RootFrame.Navigated += CompleteInitializePhoneApplication;

			// Handle navigation failures
			RootFrame.NavigationFailed += RootFrame_NavigationFailed;

			// Handle reset requests for clearing the backstack
			RootFrame.Navigated += CheckForResetNavigation;

			// Ensure we don't initialize again
			phoneApplicationInitialized = true;
		}

		// Do not add any additional code to this method
		private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
		{
			// Set the root visual to allow the application to render
			if (RootVisual != RootFrame)
				RootVisual = RootFrame;

			// Remove this handler since it is no longer needed
			RootFrame.Navigated -= CompleteInitializePhoneApplication;
		}

		private void CheckForResetNavigation(object sender, NavigationEventArgs e)
		{
			// If the app has received a 'reset' navigation, then we need to check
			// on the next navigation to see if the page stack should be reset
			if (e.NavigationMode == NavigationMode.Reset)
				RootFrame.Navigated += ClearBackStackAfterReset;
		}

		private void ClearBackStackAfterReset(object sender, NavigationEventArgs e)
		{
			// Unregister the event so it doesn't get called again
			RootFrame.Navigated -= ClearBackStackAfterReset;

			// Only clear the stack for 'new' (forward) and 'refresh' navigations
			if (e.NavigationMode != NavigationMode.New && e.NavigationMode != NavigationMode.Refresh)
				return;

			// For UI consistency, clear the entire page stack
			while (RootFrame.RemoveBackEntry() != null)
			{
				; // do nothing
			}
		}

		#endregion

		// Initialize the app's font and flow direction as defined in its localized resource strings.
		//
		// To ensure that the font of your application is aligned with its supported languages and that the
		// FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
		// and ResourceFlowDirection should be initialized in each resx file to match these values with that
		// file's culture. For example:
		//
		// AppResources.es-ES.resx
		//    ResourceLanguage's value should be "es-ES"
		//    ResourceFlowDirection's value should be "LeftToRight"
		//
		// AppResources.ar-SA.resx
		//     ResourceLanguage's value should be "ar-SA"
		//     ResourceFlowDirection's value should be "RightToLeft"
		//
		// For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
		//
		private void InitializeLanguage()
		{
			try
			{
				// Set the font to match the display language defined by the
				// ResourceLanguage resource string for each supported language.
				//
				// Fall back to the font of the neutral language if the Display
				// language of the phone is not supported.
				//
				// If a compiler error is hit then ResourceLanguage is missing from
				// the resource file.
				RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

				// Set the FlowDirection of all elements under the root frame based
				// on the ResourceFlowDirection resource string for each
				// supported language.
				//
				// If a compiler error is hit then ResourceFlowDirection is missing from
				// the resource file.
				FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection);
				RootFrame.FlowDirection = flow;
			}
			catch
			{
				// If an exception is caught here it is most likely due to either
				// ResourceLangauge not being correctly set to a supported language
				// code or ResourceFlowDirection is set to a value other than LeftToRight
				// or RightToLeft.

				if (Debugger.IsAttached)
				{
					Debugger.Break();
				}

				throw;
			}
		}
	}
}