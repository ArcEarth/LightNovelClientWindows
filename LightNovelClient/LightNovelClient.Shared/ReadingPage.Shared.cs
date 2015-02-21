using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using LightNovel.Controls;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

//using System.IO;
using System.Threading.Tasks;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Windows.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using WinRTXamlToolkit.AwaitableUI;

namespace LightNovel
{
	public class BooleanToSymbolConverter : IValueConverter
	{
		public Symbol TrueSymbol { get; set; }
		public Symbol FalseSymbol { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if ((bool)value)
				return TrueSymbol;
			else
				return FalseSymbol;

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
	/// A page that displays details for a single item within a group.
	/// </summary>
	public sealed partial class ReadingPage : Page, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public static readonly DependencyProperty ListViewScrollToIndexRequestProperty =
			DependencyProperty.Register("ListViewScrollToIndexRequest", typeof(int),
			typeof(ListViewBase), new PropertyMetadata(-1, null));
		public static readonly DependencyProperty ListViewBaseScrollToItemRequestProperty =
			DependencyProperty.Register("ListViewBaseScrollToItemRequest", typeof(object),
			typeof(ListViewBase), new PropertyMetadata(null, null));
		void ListView_SizeChanged_ScrollToItem(object sender, SizeChangedEventArgs e)
		{
			var list = sender as ListViewBase;
			if (list.Visibility == Windows.UI.Xaml.Visibility.Visible && e.NewSize.Height > 0 && list.Items.Count > 0)
			{
				var target = list.GetValue(ListViewBaseScrollToItemRequestProperty);
				if (target == null)
				{
					list.SizeChanged -= ListView_SizeChanged_ScrollToItem;
					return;
				}
				list.SelectedItem = target;
				list.UpdateLayout();
				list.ScrollIntoView(target, ScrollIntoViewAlignment.Leading);
				list.ClearValue(ListViewBaseScrollToItemRequestProperty);
				list.SizeChanged -= ListView_SizeChanged_ScrollToItem;
			}
		}

		string ImageLoadingTextPlaceholder = "Image Loading ...";

		private NavigationHelper navigationHelper;
		private NovelPositionIdentifier navigationId;

		private ReadingPageViewModel viewModel = new ReadingPageViewModel();

		public ReadingPageViewModel ViewModel
		{
			get
			{
				return viewModel;
			}
		}

		public static readonly DependencyProperty BitmapLoadingIndicatorProperty =
			DependencyProperty.Register("BitmapImageLoadingIndicator", typeof(ProgressBar),
			typeof(BitmapImage), new PropertyMetadata(null, null));

		public string IndexClosedState = "IndexClosed";
		public string IndexOpenState = "IndexOpen";

		private bool _indexOpened;
		private bool _isFisrtTimeOpenIndex = true;
		public bool IsIndexPanelOpen
		{
			get
			{
				return _indexOpened;
			}
			set
			{
				if (value == _indexOpened) return;
				_indexOpened = value;

				bool userTransition = !ViewModel.IsLoading;
				if (value)
				{
					ChangeState(IndexOpenState, userTransition);
				}
				else
				{
					ChangeState(IndexClosedState, userTransition);
				}
				NotifyPropertyChanged();
				IndexButton.IsChecked = value;
			}
		}
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}
		public Task<Chapter> LoadingAheadTask { get; set; }

		private void RefreshThemeColor()
		{
			var bgColor = ((SolidColorBrush)ViewModel.Background).Color;
			var fgColor = ((SolidColorBrush)ViewModel.Foreground).Color;
			ContentForegroundBrush.Color = fgColor;
			ContentBackgroundBrush.Color = bgColor;
			var accentColor = (Color)App.Current.Resources["AppAccentColor"];

			bgColor.R = (byte)(0.3 * bgColor.R + 0.7 * fgColor.R);
			bgColor.G = (byte)(0.3 * bgColor.G + 0.7 * fgColor.G);
			bgColor.B = (byte)(0.3 * bgColor.B + 0.7 * fgColor.B);
			//FlyoutBackgroundBrush.Color = bgColor;

			fgColor.R = (byte)(0.7 * accentColor.R + 0.3 * fgColor.R);
			fgColor.G = (byte)(0.7 * accentColor.G + 0.3 * fgColor.G);
			fgColor.B = (byte)(0.7 * accentColor.B + 0.3 * fgColor.B);
			CommentedTextBrush.Color = fgColor;

			var itemListViewSelectedBrush = (SolidColorBrush)App.Current.Resources["ListViewItemSelectedBackgroundThemeBrush"];
			itemListViewSelectedBrush.Color = ContentBackgroundBrush.Color;
			//App.Current.Resources["AppReadingBackgroundBrush"] = item.Background;
			//App.Current.Resources["AppBackgroundBrush"] = ViewModel.Background;
			//App.Current.Resources["AppForegroundBrush"] = ViewModel.Foreground;
		}

		async Task RequestCommentsInViewAsync()
		{
			var lineNo = GetCurrentLineNo();
			for (int idx = lineNo; idx < Math.Min(lineNo + 30, ViewModel.Contents.Count); idx++)
			{
				await (ViewModel.Contents[idx] as LineViewModel).LoadCommentsAsync();
			}
		}

		async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var vm = sender as ReadingPageViewModel;
			switch (e.PropertyName)
			{
				case "LineNo":
					if (!ViewModel.SuppressViewChange)
						ChangeView(-1, ViewModel.LineNo);
					break;
				case "PageNo":
					if (!ViewModel.SuppressViewChange)
						ChangeView(ViewModel.PageNo);
					break;
				case "VolumeNo":
#if WINDOWS_APP
					if (VolumeListView.Items.Count > 0 && VolumeListView.Items.Count > ViewModel.VolumeNo)
						VolumeListView.SelectedIndex = ViewModel.VolumeNo;
#endif
					SyncIndexSelection();
					LoadingAheadTask = null;
					break;
				case "ChapterNo":
#if WINDOWS_APP
					if (ChapterListView.Items.Count > 0 && ChapterListView.Items.Count > ViewModel.ChapterNo)
						ChapterListView.SelectedIndex = ViewModel.ChapterNo;
#endif
					SyncIndexSelection();
#if WINDOWS_PHONE_APP
					if (ViewModel.ChapterNo == 0)
						VerticalPrevButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
					else
						VerticalPrevButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
					if (ViewModel.ChapterNo == ViewModel.Index[ViewModel.VolumeNo].Chapters.Count - 1)
						VerticalNextButton.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
					else
						VerticalNextButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
#endif
					LoadingAheadTask = null;
					break;
				case "Contents":
					UpdateContentsView(vm.Contents.Cast<LineViewModel>());
					break;
				case "Index":
					break;
				case "IsLoading":
					if (ViewModel.IsLoading)
						VisualStateManager.GoToState(this, "Loading", true);
					else
					{
						VisualStateManager.GoToState(this, "Ready", true);
					}
					break;
				case "IsDownloading":
					{
#if WINDOWS_PHONE_APP
						//var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
						if (ViewModel.IsDownloading)
						{
							LoadingIndicator.IsIndeterminate = true;
							LoadingIndicator.Visibility = Windows.UI.Xaml.Visibility.Visible;
							//statusBar.ProgressIndicator.Text = "Caching to off-line...";
							//statusBar.ProgressIndicator.ProgressValue = null;
							//statusBar.ForegroundColor = (Color)App.Current.Resources["AppBackgroundColor"];
							//await statusBar.ShowAsync();
							//await statusBar.ProgressIndicator.ShowAsync();
						}
						else
						{
							//statusBar.ProgressIndicator.Text = " ";
							LoadingIndicator.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
							//statusBar.ProgressIndicator.ProgressValue = 0;
							//await statusBar.ProgressIndicator.HideAsync();
						}
#endif
					}
					break;
				case "IsFavored":
					FavoriteButton.IsChecked = ViewModel.IsFavored;

					break;
				case "IsPinned":
					SyncPinButtonView();
					break;
				case "DownloadingProgress":
					{
#if WINDOWS_PHONE_APP
						//var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
						//statusBar.ProgressIndicator.ProgressValue = ViewModel.DownloadingProgress;
						LoadingIndicator.IsIndeterminate = false;
						LoadingIndicator.Value = ViewModel.DownloadingProgress;
#endif
					}
					break;
				case "IsDownloaded":
					{
						var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
						if (ViewModel.IsDownloaded)
						{
							DownloadButton.IsEnabled = false;
							DownloadButton.Label = resourceLoader.GetString("DownloadButtonCompletedLabel");
						}
						else if (!ViewModel.IsDownloading)
						{
							DownloadButton.IsEnabled = true;
							DownloadButton.Label = resourceLoader.GetString("DownloadButtonStartLabel");
						}
					}

					break;
				default:
					break;
			}
		}

		private void SyncPinButtonView()
		{
			PinButton.IsChecked = ViewModel.IsPinned;
			if (ViewModel.IsPinned)
			{
				var icon = (PinButton.Content as Viewbox).Child as SymbolIcon;
				icon.Symbol = Symbol.UnPin;
			}
			else
			{
				var icon = (PinButton.Content as Viewbox).Child as SymbolIcon;
				icon.Symbol = Symbol.Pin;
			}
		}

		/// <summary>
		/// Gets the NavigationHelper used to aid in navigation and process lifetime management.
		/// </summary>


		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="sender">
		/// The source of the event; typically <see cref="NavigationHelper"/>
		/// </param>
		/// <param name="e">Event data that provides both the navigation parameter passed to
		/// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
		/// a dictionary of state preserved by this page during an earlier
		/// session.  The state will be null the first time a page is visited.</param>
		private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
		{
			// TODO: Create an appropriate data model for your problem domain to replace the sample data
			Debug.WriteLine("LoadState");
			VisualStateManager.GoToState(this, IndexClosedState, false);

#if WINDOWS_PHONE_APP
			//IndexPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
			//this.BottomAppBar = PageBottomCommandBar;
			//this.BottomAppBar.Visibility = Visibility.Visible;
			var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
			statusBar.ProgressIndicator.Text = " ";
			statusBar.ProgressIndicator.ProgressValue = 0;
			statusBar.ForegroundColor = (Color)App.Current.Resources["AppBackgroundColor"];
			await statusBar.ShowAsync();
			await statusBar.ProgressIndicator.ShowAsync();
#else
			if (this.RequestedTheme != App.Current.Settings.BackgroundTheme)
			{
				this.RequestedTheme = App.Current.Settings.BackgroundTheme;
			}
#endif
			//Relayout_ContentColumn(ContentRegion.RenderSize);
			if (e.PageState != null && e.PageState.Count > 0)
			{
				IsIndexPanelOpen = false;
				if (e.PageState.ContainsKey("LoadingBreak"))
				{
					navigationId = NovelPositionIdentifier.Parse((string)e.PageState["LoadingBreak"]);
					await viewModel.LoadDataAsync(navigationId);
				}
				else
				{
					var volumeNo = (int)e.PageState["VolumeNo"];
					var chapterNo = (int)e.PageState["ChapterNo"];
					var seriesId = (int)e.PageState["SeriesId"];
					var lineNo = (int)e.PageState["LineNo"];
					await viewModel.LoadDataAsync(seriesId, volumeNo, chapterNo, lineNo);
				}
#if WINDOWS_APP
				var horizontalOffset = (double)e.PageState["HorizontalOffset"];
				var verticalOffset = (double)e.PageState["VerticalOffset"];
				ContentScrollViewer.ChangeView(horizontalOffset, verticalOffset, null, true);
#endif
				//SyncIndexSelectedItem();
			}
			else
			{
				navigationId = NovelPositionIdentifier.Parse((string)e.NavigationParameter);

				if (navigationId.SeriesId != null && (navigationId.VolumeNo == -1 || navigationId.ChapterNo == -1))
				{
					IsIndexPanelOpen = true;
				}
				else
				{
					IsIndexPanelOpen = false;
				}

				await viewModel.LoadDataAsync(navigationId);
			}
			ViewModel.NotifyPropertyChanged("IsPinned");
			SyncPinButtonView();
			ViewModel.NotifyPropertyChanged("IsFavored");
		}
		async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
			Debug.WriteLine("Saving States");
#if WINDOWS_APP
			e.PageState.Add("HorizontalOffset", ContentScrollViewer.HorizontalOffset);
			e.PageState.Add("VerticalOffset", ContentScrollViewer.VerticalOffset);
#else
			//this.BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed; // Request to hide the bottom appbar when navigating from
#endif
			if (ViewModel.IsLoading || ViewModel.SeriesId <= 0 || !ViewModel.IsDataLoaded)
			{
				if (navigationId != null)
					e.PageState.Add("LoadingBreak", navigationId.ToString());
			}
			else
			{
#if WINDOWS_PHONE_APP
				int currentLine = GetCurrentLineNo();
				ViewModel.ReportViewChanged(null, currentLine);
#endif
				e.PageState.Add("SeriesId", ViewModel.SeriesId);
				e.PageState.Add("ChapterNo", ViewModel.ChapterNo);
				e.PageState.Add("VolumeNo", ViewModel.VolumeNo);
				e.PageState.Add("LineNo", ViewModel.LineNo);
				var bookmark = ViewModel.CreateBookmark();
				await App.Current.UpdateHistoryListAsync(bookmark);
				await App.UpdateSecondaryTileAsync(bookmark);
				if (ViewModel.IsFavored)
					await ViewModel.AddCurrentVolumeToFavoriteAsync(); // Update Favorite
				if (ViewModel.IsDownloading)
					await ViewModel.CancelCachingRequestAsync();
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

		private async void IndexButton_Click(object sender, RoutedEventArgs e)
		{
			if (IsIndexPanelOpen == false)
			{
				//IsIndexPanelOpen = true;
				SyncIndexSelection();
				//await IndexOpenStoryBoard.BeginAsync();
				IsIndexPanelOpen = true;
			}
			else
			{
				IsIndexPanelOpen = false;
			}
		}

		private async void PinButton_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as AppBarToggleButton;
			var transform = button.TransformToVisual(pageRoot);
			Point location = transform.TransformPoint(new Point(0, 0));
			if (!SecondaryTile.Exists(ViewModel.SeriesId.ToString()))
			{
				Size size = new Size(150, 150);
				var bookmark = ViewModel.CreateBookmark();
				//ViewModel.IsLoading = true;
				var imageUri = await App.CreateTileImageAsync(new Uri(bookmark.DescriptionImageUri));
				//ViewModel.IsLoading = false;
				string args = bookmark.Position.ToString();
				var tile = new SecondaryTile(ViewModel.SeriesId.ToString(), ViewModel.SeriesData.Title, args, imageUri, TileSize.Default);
				//var tile = new SecondaryTile(ViewModel.SeriesId.ToString(), "LightNovel", ViewModel.SeriesData.Title, args, TileOptions.ShowNameOnLogo, imageUri);
				button.IsChecked = await tile.RequestCreateForSelectionAsync(new Rect(location, size));
				if (button.IsChecked.Value)
				{
					var icon = (button.Content as Viewbox).Child as SymbolIcon;
					icon.Symbol = Symbol.UnPin;
				}
			}
			else
			{
				var tile = new SecondaryTile(ViewModel.SeriesId.ToString());
				button.IsChecked = !await tile.RequestDeleteAsync(location);
				if (!button.IsChecked.Value)
				{
					var icon = (button.Content as Viewbox).Child as SymbolIcon;
					icon.Symbol = Symbol.Pin;
				}
			}
		}

		private async void BookmarkButton_Click(object sender, RoutedEventArgs e)
		{

			if (!ViewModel.IsFavored)
			{
				//if (!App.Current.IsSignedIn)
				//{
				//	MessageDialog diag = new MessageDialog("登陆后才可以收藏到轻国哦:) 如果您不需要和轻国账号同步，App会自动帮您把看过的书保存在“最近”里的，这个列表会在您的设备之间自动同步！");
				//	await diag.ShowAsync();
				//	ViewModel.NotifyPropertyChanged("IsFavored");
				//	return;
				//}
				//else
				//{
				var result = await ViewModel.AddCurrentVolumeToFavoriteAsync();
				if (!result)
				{
					MessageDialog diag = new MessageDialog("收藏失败:(请检查一下网络连接再重试:)");
					await diag.ShowAsync();
				}
				//}
			}
			else
			{
				await ViewModel.RemoveCurrentVolumeFromFavoriteAsync();
			}
		}

		private void ShareButton_Click(object sender, RoutedEventArgs e)
		{
			DataTransferManager.ShowShareUI();
		}

		private void RegisterForShare()
		{
			DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
			dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager,
				DataRequestedEventArgs>(this.ShareHtmlHandler);
		}

		private void ShareHtmlHandler(DataTransferManager sender, DataRequestedEventArgs e)
		{
			DataRequest request = e.Request;
			request.Data.Properties.Title = ViewModel.VolumeData.Title;
			request.Data.Properties.Description =
				ViewModel.SeriesData.Description;
			request.Data.Properties.PackageFamilyName = "10039ArcEarth.LightNovel_9cwd8qnzd32wr";
			request.Data.Properties.ApplicationName = "LightNovel";
			var wpLink = new Uri("http://www.windowsphone.com/s?appid=c0d0077f-5426-47ee-bc97-f4c48d277095");
			request.Data.Properties.ApplicationListingUri = wpLink;
			var lkLink = new Uri("http://lknovel.lightnovel.cn/main/book/" + ViewModel.VolumeData.Id + ".html");
			request.Data.SetWebLink(lkLink);
			string html = "<h3>" + ViewModel.VolumeData.Title + "</h3><p><img src=\"" + ViewModel.VolumeData.CoverImageUri + "\"></p><p>" + ViewModel.VolumeData.Description + "</p><p>Read full article at <a href=\"" + lkLink.AbsoluteUri + "\">here</a></p>" + "<p>Download the best client for read at <a href=\"" + wpLink.AbsoluteUri + "\">Windows Phone Store</a></p>";
			string htmlFormat = HtmlFormatHelper.CreateHtmlFormat(html);
			request.Data.SetHtmlFormat(htmlFormat);
		}



		private async void IllustrationSaveButton_Click(object sender, RoutedEventArgs e)
		{
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

			MessageDialog diag = new MessageDialog(resourceLoader.GetString("ImageSaveSuccessMessage"), resourceLoader.GetString("ImageSaveMessageTitle"));

			try
			{
				var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
				var uri = new Uri(lvm.Content);
				var downloadTask = HttpWebRequest.CreateHttp(uri).GetResponseAsync();
				var localName = System.IO.Path.GetFileName(uri.LocalPath);
#if WINDOWS_APP
				var fileSavePicker = new FileSavePicker();
				fileSavePicker.FileTypeChoices.Add(".jpg Image", new List<string> { ".jpg" });
				fileSavePicker.DefaultFileExtension = ".jpg";
				fileSavePicker.SuggestedFileName = localName;
				var target = await fileSavePicker.PickSaveFileAsync();
#else
				var folder = KnownFolders.SavedPictures;
				var target = await folder.CreateFileAsync(localName, CreationCollisionOption.GenerateUniqueName);
#endif
				using (var sourceStream = (await downloadTask).GetResponseStream())
				{
					using (var targetStream = await target.OpenStreamForWriteAsync())
					{
						await sourceStream.CopyToAsync(targetStream);
					}
				}
			}
			catch (Exception)
			{
				diag.Content = resourceLoader.GetString("ImageSaveFailedMessage");
			}
			await diag.ShowAsync();
		}

		private void FontSizeButton_Click(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuFlyoutItem;
			ViewModel.FontSize = item.FontSize;

#if WINDOWS_APP
			ContentTextBlock.FontWeight = item.FontWeight;
			// Request an update of the layout since the font size is changed
			RichTextColumns.ResetOverflowLayout(ContentColumns, null);
			//ContentColumns.InvalidateMeasure();
#endif
		}
		private void FontStyleButton_Click(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuFlyoutItem;
			ViewModel.FontFamily = item.FontFamily;
		}


		private void ReadingThemButton_Click(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuFlyoutItem;
			ViewModel.Foreground = item.Foreground;
			ViewModel.Background = item.Background;
			//if (item.Text == "Dark")
			//	this.RequestedTheme = ElementTheme.Dark;
			//else
			//	this.RequestedTheme = ElementTheme.Light;
			RefreshThemeColor();

		}

		private async void CommentSubmitButton_Click(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(CommentInputBox.Text))
				return;
			var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
			var commentText = CommentInputBox.Text;
			CommentInputBox.Text = string.Empty;
			//CommentInputBox.Focus(Windows.UI.Xaml.FocusState.Unfocused);
			await lvm.AddCommentAsync(commentText);
		}

		private void CommentInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter)
			{
				CommentSubmitButton_Click(sender, null);
			}
		}

		private void IllustrationFullScreenButton_Click(object sender, RoutedEventArgs e)
		{
			var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
			((ImagePreviewFlyout.Content) as FrameworkElement).DataContext = lvm;
			Flyout.ShowAttachedFlyout(this);
		}

		private void Illustration_DownloadProgress(object sender, DownloadProgressEventArgs e)
		{
			var bitmap = sender as BitmapImage;
			var progressBar = bitmap.GetValue(BitmapLoadingIndicatorProperty) as ProgressBar;
			if (progressBar == null) return;
			progressBar.Value = e.Progress;
			if (e.Progress == 100)
			{
				var container = progressBar;//.Ge.GetVisualParent();
				if (container != null)
					container.Visibility = Visibility.Collapsed;
			}
		}

		private async void DownloadButton_Click(object sender, RoutedEventArgs e)
		{
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			if (ViewModel.IsDownloading)
			{
				DownloadButton.IsEnabled = false;
				await ViewModel.CancelCachingRequestAsync();
				DownloadButton.IsEnabled = true;
				DownloadButton.Label = resourceLoader.GetString("DownloadButtonStartLabel");
			}
			else
			{
				var pauseLabel = resourceLoader.GetString("DownloadButtonPauseLabel");
				DownloadButton.Label = pauseLabel;
				MessageDialog dialog = new MessageDialog(resourceLoader.GetString("DownloadingStartDescriptioin"), resourceLoader.GetString("DownloadingStartDescriptioinTtile"));
				var caching = ViewModel.CachingRestChaptersAsync();
				await dialog.ShowAsync();
				var result = await caching;
				if (result)
				{
					dialog.Title = resourceLoader.GetString("DownloadSuccessLabel");
					dialog.Content = resourceLoader.GetString("DownloadSuccessDescription");
					DownloadButton.IsEnabled = false;
					await dialog.ShowAsync();
				}
				else
				{
					dialog.Title = resourceLoader.GetString("DownloadFailedLabel");
					dialog.Content = resourceLoader.GetString("DownloadFailedDescription");
					DownloadButton.Label = resourceLoader.GetString("DownloadButtonCompletedLabel");
					await dialog.ShowAsync();
				}
			}
		}

		private void ClearDownloadButton_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}