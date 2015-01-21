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
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private NavigationHelper navigationHelper;
		private ObservableDictionary defaultViewModel = new ObservableDictionary();
		private ContentSectionViewModel chapterViewModel = new ContentSectionViewModel();
		private SeriesViewModel seriesViewModel = new SeriesViewModel();
		private ReadingPageViewModel viewModel = new ReadingPageViewModel();
		private NovelPositionIdentifier navigationId;

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
				if (value)
				{
					ChangeState(IndexOpenState, true);
				}
				else
				{
					ChangeState(IndexClosedState, true);
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

		void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
					VolumeListView.SelectedIndex = ViewModel.VolumeNo;
					LoadingAheadTask = null;
					break;
				case "ChapterNo":
#if WINDOWS_APP
					ChapterListView.SelectedIndex = ViewModel.ChapterNo;
#endif
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
				default:
					break;
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
			//Relayout_ContentColumn(ContentRegion.RenderSize);
			if (e.PageState == null)
			{
				var nav = navigationId = NovelPositionIdentifier.Parse((string)e.NavigationParameter);

				if (nav.SeriesId != null && (nav.VolumeNo == -1 || nav.ChapterNo == -1))
				{
					IsIndexPanelOpen = true;
				}

				await viewModel.LoadDataAsync(nav);
			}
			else
			{
				if (e.PageState.ContainsKey("LoadingBreak"))
				{
					var nav = navigationId = NovelPositionIdentifier.Parse((string)e.PageState["LoadingBreak"]);
					await viewModel.LoadDataAsync(nav);
				}
				else
				{
					var volumeNo = (int)e.PageState["VolumeNo"];
					var chapterNo = (int)e.PageState["ChapterNo"];
					var seriesId = (int)e.PageState["SeriesId"];
					await viewModel.LoadDataAsync(seriesId, volumeNo, chapterNo, null);
				}
				var horizontalOffset = (double)e.PageState["HorizontalOffset"];
				var verticalOffset = (double)e.PageState["VerticalOffset"];
#if WINDOWS_APP
				ContentScrollViewer.ChangeView(horizontalOffset, verticalOffset, null, true);
#endif
				//SyncIndexSelectedItem();
			}
			NotifyPropertyChanged("IsPinned");
			NotifyPropertyChanged("IsFavored");
		}
		async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
			Debug.WriteLine("Saving States");
#if WINDOWS_APP
			e.PageState.Add("HorizontalOffset", ContentScrollViewer.HorizontalOffset);
			e.PageState.Add("VerticalOffset", ContentScrollViewer.VerticalOffset);
#else
			this.BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed; // Request to hide the bottom appbar when navigating from
#endif
			if (ViewModel.IsLoading || ViewModel.SeriesId == 0 || !ViewModel.IsDataLoaded)
			{
				e.PageState.Add("LoadingBreak", navigationId.ToString());
			}
			else
			{
				ViewModel.ReportViewChanged(null, GetCurrentLineNo());
				e.PageState.Add("SeriesId", ViewModel.SeriesId);
				e.PageState.Add("ChapterNo", ViewModel.ChapterNo);
				e.PageState.Add("VolumeNo", ViewModel.VolumeNo);
				e.PageState.Add("LineNo", ViewModel.LineNo);
				var bookmark = ViewModel.CreateBookmark();
				await UpdateHistoryListAsync(bookmark);
				await UpdateTileAsync(bookmark);
			}
		}

		private static async Task UpdateHistoryListAsync(BookmarkInfo bookmark)
		{
			if (App.Current.RecentList == null)
				await App.Current.LoadHistoryDataAsync();
			App.Current.RecentList.RemoveAll(item => item.Position.SeriesId == bookmark.Position.SeriesId);
			App.Current.RecentList.Add(bookmark);
			App.Current.IsHistoryListChanged = true;
			await App.Current.SaveHistoryDataAsync();
		}

		private async Task<bool> UpdateTileAsync(BookmarkInfo bookmark)
		{
			if (SecondaryTile.Exists(ViewModel.SeriesId.ToString()))
			{
				var tile = new SecondaryTile(ViewModel.SeriesId.ToString());
				string args = bookmark.Position.ToString();
				tile.Arguments = args;
				var result = await tile.UpdateAsync();
				return true;
			}
			return false;
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

		private void IndexButton_Click(object sender, RoutedEventArgs e)
		{
			if (IsIndexPanelOpen == false)
			{
				IsIndexPanelOpen = true;
				SyncIndexSelection();
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
				await ViewModel.AddCurrentVolumeToFavoriteAsync();
			}
			else
			{
				await ViewModel.RemoveCurrentVolumeFromFavoriteAsync();
			}
		}

		private async void IllustrationSaveButton_Click(object sender, RoutedEventArgs e)
		{
			MessageDialog diag = new MessageDialog("Your image have saved successfully");

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
				diag.Content = "Saving file failed";
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

		private void ReadingThemButton_Click(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuFlyoutItem;
			ViewModel.Foreground = item.Foreground;
			ViewModel.Background = item.Background;
			if (item.Text == "Dark")
				this.RequestedTheme = ElementTheme.Dark;
			else
				this.RequestedTheme = ElementTheme.Light;
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
	}
}