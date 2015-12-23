using LightNovel.Common;
using LightNovel.Data;
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
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using System.Numerics;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace LightNovel
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        static Color LerpColor(Color a, Color b, double t)
        {
            double rt = 1 - t;
            return Color.FromArgb((byte)(a.A * rt + b.A * t),
                (byte)(a.R * rt + b.R * t),
                (byte)(a.G * rt + b.G * t),
                (byte)(a.B * rt + b.B * t));
        }

        /// <summary>
        /// Initializes the singleton instance of the <see cref="App"/> class. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                if (AppGlobal.Settings.BackgroundTheme == ElementTheme.Light)
                {
                    this.RequestedTheme = ApplicationTheme.Light;
                }
                else if (AppGlobal.Settings.BackgroundTheme == ElementTheme.Dark)
                {
                    this.RequestedTheme = ApplicationTheme.Dark;
                }

                var language = AppGlobal.Settings.InterfaceLanguage;
                if (!string.IsNullOrEmpty(language))
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = language;// Windows.Globalization.ApplicationLanguages.Languages[0];
                if (language == "zh-Hant")
                    LightKindomHtmlClient.UseSimplifiedCharset = false;

                this.InitializeComponent();

                this.Suspending += this.OnSuspending;
                this.Resuming += this.OnResuming;

                NetworkInformation.NetworkStatusChanged += AppGlobal.NetworkInformation_NetworkStatusChanged;
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
#if WINDOWS_UWP
            SyncAppAccentColor();
            //var search = await LightKindomHtmlClient.SearchBookAsync("青春");
            //var featured = await LightKindomHtmlClient.GetFeaturedBooks();
            //var comments = await LightKindomHtmlClient.GetCommentsAsync("797", "72");
            //var chptr = await LightKindomHtmlClient.GetChapterAsync("797", "129", "72");
            //var chptr = await LightKindomHtmlClient.GetSeriesAsync("72");

            //var chapter = new Uri("http://linovel.com/book/read?chapterId=798&volId=129&bookId=72");
            //var client = new Windows.Web.Http.HttpClient();
            //var content = await client.GetStringAsync(chapter);
            //var result = await LightKindomHtmlClient.GetCommentedLinesListAsync("4516");
            //var anotherresult = await LightKindomHtmlClient.GetCommentsAsync("36","4516");
            //var chptr = await LightKindomHtmlClient.GetSeriesIndexAsync();
            //var result = await LightKindomHtmlClient.SearchBookAsync("青春");
#endif

            mainDispatcher = Window.Current.Dispatcher;
            mainViewId = ApplicationView.GetForCurrentView().Id;

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                ExtendedSplash extendedSplash;
                if (Window.Current.Content == null)
                {
                    extendedSplash = new ExtendedSplash(e.SplashScreen);
                }
                else
                {
                    extendedSplash = Window.Current.Content as ExtendedSplash;
                }
                extendedSplash.RegisterFrameArriveDimmsion();
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = extendedSplash.RootFrame;
                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 3;

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
            //return;

            await AppGlobal.LoadHistoryDataAsync();
            await AppGlobal.LoadBookmarkDataAsync();
            await CachedClient.InitializeCachedSetAsync();
            AppGlobal.RefreshAutoLoadPolicy();
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
                        return;
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


            AppGlobal.Settings.UpdateSavedAppVersion();
            //App.CurrentState.SignInAutomaticlly();
            // Ensure the current window is active
            //Window.Current.Activate();
        }

        private void SetBrushColor(string brushName, Color color)
        {
            var brush = this.Resources[brushName] as SolidColorBrush;
            brush.Color = color;
        }

        public void SyncAppAccentColor()
        {
            Color ac, bc;
            ac = (Color) this.Resources["DefaultAppAccentColor"];
            var backgroundBrush = this.Resources["AppBackgroundBrush"] as SolidColorBrush;
            bc = backgroundBrush.Color;

            if (AppGlobal.Settings.UseSystemAccent)
            {
                var sysAccentBrush = this.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush;
                if (sysAccentBrush != null )
                {
                    ac = sysAccentBrush.Color;
                }
            }

            var slac = LerpColor(ac, bc, 0.3);
            var lac = LerpColor(ac, bc, 0.5);
            this.Resources["AppAccentColor"] = ac;
            this.Resources["AppAccentColorSemiLight"] = slac;
            this.Resources["AppAccentColorLight"] = lac;

            SetBrushColor("AppAccentBrush", ac);
            SetBrushColor("AppAccentBrushSemiLight", slac);
            SetBrushColor("AppAccentBrushLight", lac);

            // Override system default control brushes
            SetBrushColor("ToggleSwitchCurtainBackgroundThemeBrush", ac);
            SetBrushColor("ComboBoxItemSelectedForegroundThemeBrush", ac);
            SetBrushColor("ComboBoxHighlightedBorderThemeBrush", ac);
            SetBrushColor("ComboBoxPressedBackgroundThemeBrush", ac);
            SetBrushColor("PhoneAccentBrush", ac);
            SetBrushColor("ProgressBarIndeterminateForegroundThemeBrush", ac);
            SetBrushColor("PhoneHighContrastSelectedForegroundThemeBrush", ac);
            SetBrushColor("AppBarToggleButtonCheckedPointerOverBackgroundThemeBrush", lac);
            SetBrushColor("AppBarToggleButtonCheckedPressedBackgroundThemeBrush", slac);
            SetBrushColor("AppBarToggleButtonCheckedForegroundThemeBrush", ac);

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            ApplicationView.GetForCurrentView().FullScreenSystemOverlayMode = FullScreenSystemOverlayMode.Minimal;
            //ApplicationView.GetForCurrentView().SuppressSystemOverlays = true;

            //Setup Title bar colors
            titleBar.BackgroundColor = Colors.Transparent;
            titleBar.ButtonBackgroundColor = ac;
            titleBar.ButtonForegroundColor = bc;
            titleBar.ButtonInactiveBackgroundColor = ac;
            titleBar.ButtonInactiveForegroundColor = bc;
            titleBar.ButtonHoverForegroundColor = bc;
            titleBar.ButtonHoverBackgroundColor = slac;
            titleBar.ButtonPressedForegroundColor = bc;
            titleBar.ButtonPressedBackgroundColor = lac;

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
#elif WINDOWS_APP
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