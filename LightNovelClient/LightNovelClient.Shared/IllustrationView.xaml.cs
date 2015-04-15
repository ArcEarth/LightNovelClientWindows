using LightNovel.Common;
using LightNovel.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.AwaitableUI;
using WinRTXamlToolkit.Controls.Extensions;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace LightNovel
{
	public sealed partial class IllustrationView : Grid
	{
		public IllustrationView()
		{
			this.InitializeComponent();
		}

		public void ClearContent(string textPlaceholder)
		{
			TextContent.Text = textPlaceholder;
			ImagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Visible;
			ImagePlaceHolder.Opacity = 1;
			ImageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
			TextContent.TextAlignment = TextAlignment.Center;
			TextContent.Opacity = 1;
			ImageContent.Opacity = 0;
			ProgressBar.Opacity = 1;
		}

		public void LoadIllustrationLine(LineViewModel line)
		{
			var bitMap = new BitmapImage(line.ImageUri);
			DataContext = line;
			bitMap.DownloadProgress += Image_DownloadProgress;
			ImageContent.ImageOpened += imageContent_ImageOpened;
			ImageContent.Source = bitMap;
		}

		void imageContent_ImageOpened(object sender, RoutedEventArgs e)
		{
			ImageContent.ImageOpened -= imageContent_ImageOpened;
			ImagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			ImageContent.FadeInCustom(new TimeSpan(0, 0, 0, 0, 500), null, 1);
		}

		private async void ImageContent_Failed(object sender, ExceptionRoutedEventArgs e)
		{
			await RefreshCruptedImage();
		}

		private void Image_DownloadProgress(object sender, DownloadProgressEventArgs e)
		{
			var bitmap = sender as BitmapImage;
			ProgressBar.Value = e.Progress;
			if (e.Progress == 100)
			{
				TextContent.Opacity = 0;
				ProgressBar.Opacity = 0;
			}
		}

		public async Task RefreshCruptedImage()
		{

			var lvm = DataContext as LineViewModel;
			if (lvm == null) return;

			var bitmap = ImageContent.Source as BitmapImage;
			if (bitmap == null) return;

			var uri = bitmap.UriSource.AbsoluteUri;
			if (uri.StartsWith("ms-appdata"))
			{
				var remoteUri = lvm.Content;

				var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
				TextContent.Text = resourceLoader.GetString("ImageLoadingPlaceholderText"); ;
				ImagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Visible;
				ImagePlaceHolder.Opacity = 1;
				ImageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
				TextContent.TextAlignment = TextAlignment.Center;
				TextContent.Opacity = 1;
				ImageContent.Opacity = 0;
				ProgressBar.Opacity = 1;

				ImageContent.ImageOpened -= imageContent_ImageOpened;
				bitmap.DownloadProgress -= Image_DownloadProgress;

				bitmap.UriSource = new Uri(remoteUri);
				bitmap.DownloadProgress += Image_DownloadProgress;
				ImageContent.ImageOpened += imageContent_ImageOpened;

				await CachedClient.DeleteIllustationAsync(remoteUri);
			} // else is Network Issue
		}
	}
}
