using LightNovel.Common;
using LightNovel.Controls;
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
        static string _ImageLoadingTextPlaceholder = "Image Loading ...";
        static string _ImageTapToLoadPlaceholder = "Tap to load Illustration";
        static string _ImageLoadFailedPlaceholder = "Illustration failed to load. Tap to retry.";

        static public string ImageLoadingTextPlaceholder { get { return _ImageLoadingTextPlaceholder; } set { _ImageLoadingTextPlaceholder = value; } }
        static public string ImageTapToLoadPlaceholder { get { return _ImageTapToLoadPlaceholder; } set { _ImageTapToLoadPlaceholder = value; } }
        static public string ImageLoadFailedPlaceholder { get { return _ImageLoadFailedPlaceholder; } set { _ImageLoadFailedPlaceholder = value; } }

        public static readonly DependencyProperty BitmapLoadingIndicatorProperty =
            DependencyProperty.Register("BitmapImageLoadingIndicator", typeof(ProgressBar),
            typeof(BitmapImage), new PropertyMetadata(null, null));

        public IllustrationView()
        {
            this.InitializeComponent();
            ImageContent.ImageOpened += ImageContent_ImageOpened;
            ImageContent.ImageFailed += ImageContent_Failed;
        }

        public void ClearContent()
        {
            ProgressBar.Value = 0;
            if (ImageContent.Source != null)
            {
                var bitmap = ImageContent.Source as BitmapImage;
                bitmap.DownloadProgress -= Image_DownloadProgress;
            }
            ImageContent.DataContext = null;
            ImageContent.ClearValue(Image.SourceProperty);
            ImageContent.Visibility = Visibility.Collapsed;
            ImageContent.Height = 0;
            TextContent.ClearValue(TextBlock.TextProperty);
        }

        public void Phase0(LineViewModel line)
        {
            var iv = LayoutRoot;
            ImageContent.Opacity = 0;
            ImageContent.Height = double.NaN;
            CommentIndicator.Opacity = 0;
            ProgressBar.Opacity = 0;
            TextContent.Opacity = 1;
            if (ImageContent.Source != null)
            {
                var bitmap = ImageContent.Source as BitmapImage;
                bitmap.DownloadProgress -= Image_DownloadProgress;
                ImageContent.ClearValue(Image.SourceProperty);
            }

            if (line.IsImage)
            {
                if (!AppGlobal.ShouldAutoLoadImage)
                    TextContent.Text = ImageTapToLoadPlaceholder;
                else
                    TextContent.Text = ImageLoadingTextPlaceholder;

                double aspect = line.ImageWidth <= 0? .0 : (double)line.ImageHeight / (double)line.ImageWidth;
                double ih = iv.Width * aspect;

                if (ih > 1.0)
                {
                    ImageContent.Height = ih;
                    ImagePlaceHolder.Height = ih;
                }
                else
                {
                    ImagePlaceHolder.Height = double.NaN;
                }

                ProgressBar.Visibility = Visibility.Visible;
                ImageContent.Visibility = Visibility.Visible;
                ImagePlaceHolder.Visibility = Visibility.Visible;
                TextContent.TextAlignment = TextAlignment.Center;
            }
            else
            {
                TextContent.Text = "　" + line.Content;
                //textContent.Height = double.NaN;
                TextContent.TextAlignment = TextAlignment.Left;

                ImagePlaceHolder.Visibility = Visibility.Collapsed;
                ImageContent.Visibility = Visibility.Collapsed;
                ImageContent.DataContext = null;
            }
        }

        public void Phase1(LineViewModel line)
        {
            CommentIndicator.Opacity = line.HasComments ? 1 : 0;

            if (line.IsImage)
            {
                ProgressBar.Value = 0;
                ProgressBar.Opacity = 1;
            }
        }

        // aka Phase 2
        public async Task LoadIllustrationLine(LineViewModel line)
        {
            var bitMap = new BitmapImage();
            if (line.ImageHeight > 1.0)
            {
                bitMap.DecodePixelHeight = line.ImageHeight;
                bitMap.DecodePixelWidth = line.ImageWidth;
                bitMap.DecodePixelType = DecodePixelType.Physical;
            }

            ImageContent.DataContext = line;

            bitMap.SetValue(BitmapLoadingIndicatorProperty, ProgressBar);
            bitMap.DownloadProgress += Image_DownloadProgress;

            //ResetPhase0(line);
            //CommentIndicator.Opacity = line.HasComments ? 1 : 0;
            //ProgressBar.Visibility = Visibility.Visible;
            //ImagePlaceHolder.Visibility = Visibility.Visible;
            //ProgressBar.Value = 0;

            try
            {
                var download = line.DownloadImageAsync();
                download.Progress = async (info, p) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () => SetDownloadProgress(p));
                };

                var stream = await download;
                var setTask = bitMap.SetSourceAsync(stream);
                ImageContent.Source = bitMap;
                await setTask;
            }
            catch (Exception excp)
            {
                ImageContent_Failed(ImageContent, null);
            }
        }

        public async void ImageContent_ImageOpened(object sender, RoutedEventArgs e)
        {
            //ImageContent.ImageOpened -= ImageContent_ImageOpened;
            var lvm = ImageContent.DataContext as LineViewModel;
            if (lvm != null && lvm.IsImage)
            {
                TextContent.Opacity = 0;
                ProgressBar.Opacity = 0;
                //ProgressBar.Visibility = Visibility.Collapsed;
                ImagePlaceHolder.Visibility = Visibility.Collapsed;
                await ImageContent.FadeInCustomAsync(new TimeSpan(0, 0, 0, 0, 500), null, 1);
            }
        }

        private void ImageContent_Failed(object sender, ExceptionRoutedEventArgs e)
        {
            var lvm = ImageContent.DataContext as LineViewModel;
            if (lvm != null && lvm.IsImage)
            {
                ProgressBar.Value = 0;
                TextContent.Text = ImageLoadFailedPlaceholder;
            }
            //await RefreshCruptedImage();
        }

        private void Image_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            var bitmap = sender as BitmapImage;
            var ProgressBar = bitmap.GetValue(BitmapLoadingIndicatorProperty) as ProgressBar;
            if (ProgressBar == null) return;
            SetDownloadProgress(e.Progress);
        }

        private void SetDownloadProgress(int progress)
        {
            ProgressBar.Value = progress;
            if (progress == 100)
            {
                //var iv = ProgressBar.GetVisualParent();
                //if (iv == null) return;
                TextContent.Opacity = 0;
                ProgressBar.Opacity = 0;
                //imageContent.Visibility = Visibility.Visible;
            }
        }

        public async Task RefreshCruptedImage()
        {

            var lvm = DataContext as LineViewModel;
            if (lvm == null) return;

            var bitmap = ImageContent.Source as BitmapImage;
            if (bitmap == null) return;

            if (lvm.IsImageCached)
            {
                var remoteUri = lvm.Content;

                TextContent.Text = ImageLoadingTextPlaceholder;
                ImagePlaceHolder.Visibility = Visibility.Visible;
                ImagePlaceHolder.Opacity = 1;
                ImageContent.Visibility = Visibility.Visible;
                TextContent.TextAlignment = TextAlignment.Center;
                TextContent.Opacity = 1;
                ImageContent.Opacity = 0;
                ProgressBar.Opacity = 1;

                //ImageContent.ImageOpened -= ImageContent_ImageOpened;
                bitmap.DownloadProgress -= Image_DownloadProgress;
                bitmap.UriSource = new Uri(remoteUri);
                bitmap.DownloadProgress += Image_DownloadProgress;
                //ImageContent.ImageOpened += ImageContent_ImageOpened;

                await lvm.Client.DeleteIllustationAsync(remoteUri);
            } // else is Network Issue
        }
    }
}
