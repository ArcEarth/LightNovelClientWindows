// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

using LightNovel;
using LightNovel.Common;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.AwaitableUI;

namespace LightNovel
{
    partial class ExtendedSplash
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.

		private Frame rootFrame;

		public Frame RootFrame { get { return rootFrame; } }

        private SplashScreen splash; // Variable to hold the splash screen object.

        public ExtendedSplash(SplashScreen splashscreen)
        {
            InitializeComponent();

            // Listen for window resize events to reposition the extended splash screen image accordingly.
            // This is important to ensure that the extended splash screen is formatted properly in response to snapping, unsnapping, rotation, etc...
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(ExtendedSplash_OnResize);

            splash = splashscreen;

            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, Object>(DismissedEventHandler);

                // Retrieve the window coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }

            // Create a Frame to act as the navigation context
			rootFrame = new Frame() { Background = (SolidColorBrush)App.Current.Resources["AppBackgroundBrush"] };
			rootFrame.Navigated += rootFrame_Navigated;
			//DissmissStory.Begin();
		}

		public void RegisterFrameArriveDimmsion()
		{
			//rootFrame.Navigated -= rootFrame_Navigated;
			rootFrame.Navigated += rootFrame_Navigated;
		}

        // Position the extended splash screen image in the same location as the system splash screen image.
        void PositionImage()
        {
			//extendedSplashImage.SetValue(Viewbox.HeightProperty, splashImageRect.Height);
			//extendedSplashImage.SetValue(Viewbox.WidthProperty, splashImageRect.Width);
        }

        void ExtendedSplash_OnResize(Object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
            if (splash != null)
            {
                // Update the coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionImage();
            }
        }

		void rootFrame_Navigated(object sender, NavigationEventArgs e)
		{
			rootFrame.Navigated -= rootFrame_Navigated;
			//SplashElements.Opacity = 0;
			Window.Current.Content = rootFrame;
		}

        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        void DismissedEventHandler(SplashScreen sender, object e)
        {
            dismissed = true;

            // Navigate away from the app's extended splash screen after completing setup operations here...
            // This sample navigates away from the extended splash screen when the "Learn More" button is clicked.
        }
    }
}
