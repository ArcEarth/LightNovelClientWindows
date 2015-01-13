using LightNovel.Common;
using LightNovel.Controls;
using LightNovel.Service;
using LightNovel.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;
//using PostSharp.Patterns.Model;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

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

		/// <summary>
		/// Identifies the <see cref="ParagrahNoProperty"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ParagrahViewModelProperty =
			DependencyProperty.Register("ParagrahViewModel", typeof(LineViewModel),
			typeof(Paragraph), new PropertyMetadata(null, null));

		public static readonly DependencyProperty BitmapLoadingIndicatorProperty =
			DependencyProperty.Register("BitmapImageLoadingIndicator", typeof(ProgressBar),
			typeof(BitmapImage), new PropertyMetadata(null, null));



		public ReadingPageViewModel ViewModel
		{
			get
			{
				return viewModel;
			}
		}

		public string IndexClosedState = "IndexClosed";
		public string IndexOpenState = "IndexOpen";

		//private EasingDoubleKeyFrame IndexCollapsedToExpandedKeyFrame;
		//private EasingDoubleKeyFrame ContentCollapsedToExpandedKeyFrame;
		//private EasingDoubleKeyFrame IndexExpandedToCollapsedKeyFrame;
		//private EasingDoubleKeyFrame ContentExpandedToCollapsedKeyFrame;

		private double PictureMargin = 8;
		private int ColumnsPerScreen = 2;
		//private int MinmumColumnWidth = 500;

		private int MinimumWidthForSupportingTwoPanes = 768;
		public bool UsingLogicalIndexPage
		{
			get
			{
				return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes;
			}
		}

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

		public ReadingPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
			this.navigationHelper.GoBackCommand = new LightNovel.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
			ScrollSwitch = false;
			DisableAnimationScrollingFlag = true;
			ContentRegion.SizeChanged += ContentRegion_SizeChanged;
			ContentColumns.ColumnsChanged += ContentColumns_LayoutUpdated;
			ContentScrollViewer.LayoutUpdated += ScrollToPage_ContentColumns_LayoutUpdated;
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
			ViewModel.CommentsListLoaded += ViewModel_CommentsListLoaded;
			Flyout.SetAttachedFlyout(this, ImagePreviewFlyout);
			RefreshThemeColor();
		}

		private void RefreshThemeColor()
		{
			var bgColor = ((SolidColorBrush)ViewModel.Background).Color;
			var fgColor = ((SolidColorBrush)ViewModel.Foreground).Color;
			ContentTextBrush.Color = fgColor;
			ContentBackgroundBrush.Color = bgColor;
			var accentColor = (Color)App.Current.Resources["AppAccentColor"];

			bgColor.R = (byte)(0.8 * bgColor.R + 0.2 * fgColor.R);
			bgColor.G = (byte)(0.8 * bgColor.G + 0.2 * fgColor.G);
			bgColor.B = (byte)(0.8 * bgColor.B + 0.2 * fgColor.B);
			bgColor.A = 0xBB;
			FlyoutBackgroundBrush.Color = bgColor;

			fgColor.R = (byte)(0.7 * accentColor.R + 0.3 * fgColor.R);
			fgColor.G = (byte)(0.7 * accentColor.G + 0.3 * fgColor.G);
			fgColor.B = (byte)(0.7 * accentColor.B + 0.3 * fgColor.B);
			CommentedTextBrush.Color = fgColor;
		}

		void RequestCommentsInView()
		{
			//var lineNo = GetCurrentLineNo();
			//for (int idx = lineNo; idx < Math.Min(lineNo + 30, ViewModel.Contents.Count); idx++)
			//{
			//	ViewModel.Contents[idx].LoadCommentsAsync();
			//}
		}

		void ViewModel_CommentsListLoaded(object sender, IEnumerable<int> e)
		{
			foreach (int idx in e)
			{
				var para = ContentTextBlock.Blocks[idx-1];
				//var fcolor = (ViewModel.Foreground as SolidColorBrush).Color;
				para.Foreground = CommentedTextBrush; //(SolidColorBrush)App.Current.Resources["AppAccentBrushLight"];
			}
			RequestCommentsInView();
		}

		void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var vm = sender as ReadingPageViewModel;
			switch (e.PropertyName)
			{
				case "LineNo":
					if (!ViewModel.SuppressViewChange)
					{
						var page = GetPageNoFromLineNo(ViewModel.LineNo);
						if (page >= 0)
							ViewModel.PageNo = page;
						else
						{
							RequestChangeView(null, ViewModel.LineNo);
						}
					}
					break;
				case "PageNo":
					if (!ViewModel.SuppressViewChange)
						if (!ContentColumns.IsLayoutValiad || ContentColumns.Visibility != Windows.UI.Xaml.Visibility.Visible)
						{
							RequestChangeView(ViewModel.PageNo, null);
						}
						else
						{
							ScrollToPage(ViewModel.PageNo, DisableAnimationScrollingFlag);
							DisableAnimationScrollingFlag = true;
						}
					break;
				case "VolumeNo":
					VolumeListView.SelectedIndex = ViewModel.VolumeNo;
					LoadingAheadTask = null;
					break;
				case "ChapterNo":
					ChapterListView.SelectedIndex = ViewModel.ChapterNo;
					LoadingAheadTask = null;
					break;
				case "Contents":
					UpdateContentsView(vm.Contents);
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
		private bool CanGoBack()
		{
			if (this.UsingLogicalIndexPage && IsIndexPanelOpen && ChapterListView.SelectedItem != null)
			{
				return true;
			}
			else
			{
				return this.navigationHelper.CanGoBack();
			}
		}
		private void GoBack()
		{
			if (this.UsingLogicalIndexPage && IsIndexPanelOpen && ChapterListView.SelectedItem != null)
			{
				// When logical page navigation is in effect and there's a selected item that
				// item's details are currently displayed.  Clearing the selection will return to
				// the item list.  From the user's point of view this is a logical backward
				// navigation.
				IsIndexPanelOpen = false;
			}
			else
			{
				this.navigationHelper.GoBack();
			}
		}

		void ChangeState(string stateName, bool useTransitions = true)
		{
			IndexCollapsedToExpandedKeyFrame.Value = -IndexRegion.Width;
			ContentCollapsedToExpandedKeyFrame.Value = -IndexRegion.Width;
			IndexExpandedToCollapsedKeyFrame.Value = -IndexRegion.Width;
			ContentExpandedToCollapsedKeyFrame.Value = -IndexRegion.Width;
			VisualStateManager.GoToState(this, stateName, useTransitions);
			this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
		}

		void ChangeContentFlowDirection(Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
			{
				ContentColumns.Orientation = Orientation.Vertical;
				ContentColumns.Margin = new Thickness(0, 100, 0, 100);
				ContentScrollViewer.Style = (Style)App.Current.Resources["VerticalScrollViewerStyle"];
				ContentScrollViewer.HorizontalSnapPointsType = SnapPointsType.None;
				ContentScrollViewer.VerticalSnapPointsType = SnapPointsType.Mandatory;
			}
			else
			{
				ContentColumns.Orientation = Orientation.Horizontal;
				ContentColumns.Margin = new Thickness(100, 0, 100, 0);
				ContentScrollViewer.Style = (Style)App.Current.Resources["HorizontalScrollViewerStyle"];
				ContentScrollViewer.HorizontalSnapPointsType = SnapPointsType.Mandatory;
				ContentScrollViewer.VerticalSnapPointsType = SnapPointsType.None;
			}
		}

		void Relayout_ContentImages()
		{
			foreach (Paragraph para in ContentColumns.RichTextContent.Blocks)
			{
				foreach (var inline in para.Inlines)
				{
					var container = inline as InlineUIContainer;
					if (container != null)
					{
						var elem = container.Child as FrameworkElement;
						Size padding = new Size(ContentTextBlock.Padding.Left + ContentTextBlock.Padding.Right, ContentTextBlock.Padding.Top + ContentTextBlock.Padding.Bottom);
						elem.Width = ContentColumns.ColumnWidth - padding.Width - 1;
						elem.Height = ContentColumns.ColumnHeight - padding.Height - PictureMargin;
					}
				}
			}
			RichTextColumns.ResetOverflowLayout(ContentColumns, null);
		}

		void Relayout_ContentColumn(Size displaySize)
		{
			if (displaySize.Width <= displaySize.Height * 1.1)
				ColumnsPerScreen = 1;
			else
				ColumnsPerScreen = 2;

			if (displaySize.Width < 200)
			{
				ColumnsPerScreen = 0;
			}

			// If not Visible
			if (ColumnsPerScreen <= 0)
			{
				ContentColumns.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				Debug.WriteLine("Content Columns Width is less than 200, this should be impossible to happen in reality use.");
				return;
			}
			else
				ContentColumns.Visibility = Windows.UI.Xaml.Visibility.Visible;

			ContentColumns.ColumnWidth = displaySize.Width / ColumnsPerScreen;
			ContentColumns.ColumnHeight = displaySize.Height;
			if (ContentColumns.Visibility != Windows.UI.Xaml.Visibility.Collapsed)
				ChangeContentFlowDirection(ColumnsPerScreen <=1 ? Orientation.Vertical:Orientation.Horizontal);
			Relayout_ContentImages();
		}
		private void ContentRegion_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var currentLine = ViewModel.LineNo;
			Debug.WriteLine("ContentRegion_SizeChanged, lineNo: {0}", currentLine);
			//var displaySize = new Size(e.NewSize.Width, e.NewSize.Height);
			Relayout_ContentColumn(e.NewSize);
			RequestChangeView(null,currentLine);
		}

		void ContentColumns_LayoutUpdated(object sender, object e)
		{
			Debug.WriteLine("ContentColumns_LayoutUpdated.");
			ViewModel.PagesCount = ContentColumns.Children.Count;
		}

		/// <summary>
		/// Gets the NavigationHelper used to aid in navigation and process lifetime management.
		/// </summary>
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}

		//public SeriesViewModel SeriesViewModel
		//{
		//	get { return chapterViewModel; }
		//}

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
				ContentScrollViewer.ChangeView(horizontalOffset, verticalOffset, null, true);
				//SyncIndexSelectedItem();
			}
			NotifyPropertyChanged("IsPinned");
			NotifyPropertyChanged("IsFavored");
		}
		async void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
			Debug.WriteLine("Saving States");
			e.PageState.Add("HorizontalOffset", ContentScrollViewer.HorizontalOffset);
			e.PageState.Add("VerticalOffset", ContentScrollViewer.VerticalOffset);
			if (ViewModel.IsLoading || ViewModel.SeriesId == 0 || !ViewModel.IsDataLoaded)
			{
				e.PageState.Add("LoadingBreak", navigationId.ToString());
			}
			else
			{
				e.PageState.Add("SeriesId", ViewModel.SeriesId);
				e.PageState.Add("ChapterNo", ViewModel.ChapterNo);
				e.PageState.Add("VolumeNo", ViewModel.VolumeNo);
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

		public bool ScrollSwitch { get; set; }
		public bool UseTargetLine { get; set; }
		public int TargetLineNo { get; set; }
		public int TargetPageNo { get; set; }

		void RequestChangeView(int? page, int? line)
		{
			if (page != null && line.Value >= 0)
			{
				ScrollSwitch = true;
				UseTargetLine = false;
				TargetPageNo = page.Value;
			}
			else if (line != null && line.Value >= 0)
			{
				ScrollSwitch = true;
				UseTargetLine = true;
				TargetLineNo = line.Value;
			}
		}

		void ScrollToPage(int page, bool disableAnimation)
		{
			if (ContentScrollViewer.HorizontalScrollMode != ScrollMode.Disabled)
				ContentScrollViewer.ChangeView(ContentColumns.Margin.Left + page * ContentColumns.ColumnWidth + 0.1, null, null, disableAnimation);
			else
				ContentScrollViewer.ChangeView(null, ContentColumns.Margin.Top + page * ContentColumns.ColumnHeight + 0.1, null, disableAnimation);
		}

		void ScrollToPage_ContentColumns_LayoutUpdated(object sender, object e)
		{
			//Debug.WriteLine("ScrollViewer_LayoutUpdated , Size ({0},{1})", ContentScrollViewer.ActualWidth,ContentScrollViewer.ActualHeight);
			if (!ScrollSwitch || !ContentColumns.IsLayoutValiad || ContentColumns.Visibility != Windows.UI.Xaml.Visibility.Visible || TotalPage <= 0)
				return;
			if (UseTargetLine)
			{
				ViewModel.PageNo = GetPageNoFromLineNo(TargetLineNo);
			}
			else
			{
				ScrollToPage(TargetPageNo, true);
			}
			ScrollSwitch = false;
			Debug.WriteLine("Delayed Scroll Appears!");
		}

		private void UpdateContentsView(IEnumerable<LineViewModel> lines)
		{
			Uri severBaseUri = new Uri("http://lknovel.lightnovel.cn");
			ContentTextBlock.Blocks.Clear();
			bool prevLineBreakFlag = false;
			foreach (var line in lines)
			{
				var para = new Paragraph();
				para.SetValue(ParagrahViewModelProperty, line);
				if (line.ContentType == LineContentType.TextContent || line.Content == null)
				{
					if (line.HasComments)
						para.Inlines.Add(new InlineUIContainer
						{
							Child = new SymbolIcon { Symbol = Symbol.Message },
							Foreground = (SolidColorBrush)App.Current.Resources["AppAcentBrush"]
						});
					var run = new Run { Text = line.Content };
					para.Inlines.Add(run);
					para.TextIndent = ContentTextBlock.FontSize * 2;
					prevLineBreakFlag = true;
					para.Margin = new Thickness(0, 0, 0, 10);
				}
				else
				{
					//para.LineHeight = 2;
					Size padding = new Size(ContentTextBlock.Padding.Left + ContentTextBlock.Padding.Right, ContentTextBlock.Padding.Top + ContentTextBlock.Padding.Bottom);
					//bitmap.DownloadProgress +=
					//var img = new Image
					//{
					//	Source = bitmap,
					//	//MaxWidth = ContentColumns.ColumnWidth - padding.Width - 1,
					//	//Height = ContentColumns.ColumnHeight - padding.Height - PictureMargin,
					//	HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
					//	VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
					//	Stretch = Stretch.Uniform,
					//};
					//img.DataContext = img;
					//Flyout.SetAttachedFlyout(img, this.Resources["ImagePreviewFlyout"] as Flyout);
					//img.Tapped += Illustration_Tapped;
					//GetLocalImageAsync(new Uri(severBaseUri, line.Content)).ContinueWith(async (task) =>
					//{
					//	await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,()=>{
					//		if (task.IsFaulted || task.Result == null)
					//		{
					//			img.Source = new BitmapImage(new Uri(severBaseUri, line.Content));
					//		}
					//		else
					//		{
					//			var localUri = task.Result;
					//			img.Source = new BitmapImage(localUri);
					//		}
					//	});
					//});


					//var illustration = new Border
					//{
					//	HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
					//	VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch,
					//	Width = ContentColumns.ColumnWidth - padding.Width - 1,
					//	Height = ContentColumns.ColumnHeight - padding.Height - PictureMargin,
					//	Background = null,
					//	BorderBrush = null,
					//	Child = img,
					//};

					var illustration = IllustrationViewTemplate.LoadContent() as Grid;
					illustration.DataContext = line;
					illustration.Width = ContentColumns.ColumnWidth - padding.Width - 1;
					illustration.Height = ContentColumns.ColumnHeight - padding.Height - PictureMargin;
					var bitmap = (illustration.GetFirstDescendantOfType<Image>().Source as BitmapImage);
					var pb = illustration.GetFirstDescendantOfType<ProgressBar>();
					bitmap.SetValue(BitmapLoadingIndicatorProperty, pb);

					var inlineImg = new InlineUIContainer
					{
						Child = illustration // img
					};

					//inlineImg.FontSize = 620;
					para.TextAlignment = TextAlignment.Center;
					if (prevLineBreakFlag)
					{
						para.Inlines.Add(new Run { Text = "\n" });
						illustration.Margin = new Thickness(0, 5, 0, 0);
						//img.Margin = new Thickness(0, 5, 0, 0);
					}
					else
					{
						para.Inlines.Add(new Run { Text = " \n", FontSize = 5 });
					}
					para.Inlines.Add(inlineImg);

					prevLineBreakFlag = false;
				}
				ContentTextBlock.Blocks.Add(para);
			}
			var ptr = ContentTextBlock.ContentStart;
			//ContentColumns.Measure(new Size(ContentScrollViewer.Height, double.PositiveInfinity));
			//ContentColumns.Children.Count;
			RichTextColumns.ResetOverflowLayout(ContentColumns, null);
		}

		void Illustration_Tapped(object sender, TappedRoutedEventArgs e)
		{
			//PageBottomCommandBar.IsOpen = true;
			//PageBottomCommandBar.IsEnabled = true;
			Flyout.ShowAttachedFlyout((FrameworkElement)sender);
		}

		private async void PrevChapterButton_Click(object sender, RoutedEventArgs e)
		{
			int page = GetCurrentPageNo();
			if (page == 0)
			{
				if (ViewModel.ChapterNo > 0 && !ViewModel.IsLoading)
				{
					await ViewModel.LoadDataAsync(null, null, ViewModel.ChapterNo - 1, null);
					ViewModel.PageNo = TotalPage - 2;
				}
			}
			else
			{
				page -= ColumnsPerScreen;
				page = Math.Max(0, page);
				DisableAnimationScrollingFlag = false;
				ViewModel.PageNo = page;
			}
		}

		private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
		{
			int page = GetCurrentPageNo();
			if (page >= TotalPage - ColumnsPerScreen)
			{
				if (ViewModel.ChapterNo < ViewModel.Index[ViewModel.VolumeNo].Chapters.Count - 1 && !ViewModel.IsLoading)
				{
					await ViewModel.LoadDataAsync(null, null, ViewModel.ChapterNo + 1, null);
					ViewModel.PageNo = 0;
				}
			}
			else
			{
				page += ColumnsPerScreen;
				page = Math.Min(TotalPage - 1, page);
				DisableAnimationScrollingFlag = false;
				ViewModel.PageNo = page;
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

		private void ContentScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			int page = GetCurrentPageNo();
			ViewModel.ReportViewChanged(page, null);

			if (TotalPage - page < 50 && (LoadingAheadTask == null || LoadingAheadTask.IsFaulted) && !string.IsNullOrEmpty(ViewModel.ChapterData.NextChapterId))
				LoadingAheadTask = CachedClient.GetChapterAsync(ViewModel.ChapterData.NextChapterId);

			//double offsetRatio = 0;
			if (page == TotalPage - ColumnsPerScreen)
			{
				//if (ContentColumns.Orientation == Orientation.Horizontal)
				//{
				//	double blockWidith = (TotalPage - ColumnsPerScreen) * ContentColumns.ColumnWidth;
				//	offsetRatio = (ContentScrollViewer.HorizontalOffset - ContentColumns.Margin.Left - blockWidith) / ContentColumns.Margin.Right;
				//}
				//else
				//{
				//	double blockWidith = (TotalPage - ColumnsPerScreen) * ContentColumns.ColumnHeight;
				//	offsetRatio = (ContentScrollViewer.VerticalOffset - ContentColumns.Margin.Top - blockWidith) / ContentColumns.Margin.Right;
				//}
				//Color col = (Color)Application.Current.Resources["AppAccentColor"];
				//col.A = (byte)((offsetRatio)*255);

				//HorizontalNextButton.Width = 30 + 50 * offsetRatio;
				//var trans = (((HorizontalNextButton.GetChildren().First() as Border).Child as Windows.UI.Xaml.Shapes.Path).RenderTransform as CompositeTransform);
				//if (trans == null)
				//{
				//	trans = new CompositeTransform();
				//	((HorizontalNextButton.GetChildren().First() as Border).Child as Windows.UI.Xaml.Shapes.Path).RenderTransform = trans;
				//}
				//trans.ScaleX = (1 + offsetRatio);
				//trans.ScaleY = (1 + offsetRatio);
				//trans.Rotation = 360 * offsetRatio;
				//((HorizontalNextButton.Background) as SolidColorBrush).Color = col;

				//VerticalNextButton.Height = 30 + 50 * offsetRatio;
				//((VerticalNextButton.Background) as SolidColorBrush).Color = col;

				//if (!e.IsIntermediate)
				//{
				//	NextChapterButton_Click(null, null);
				//}

				VisualStateManager.GoToState(HorizontalNextButton, "Highlight", false);
				VisualStateManager.GoToState(VerticalNextButton, "Highlight", false);
			}
			else
			{
				VisualStateManager.GoToState(HorizontalNextButton, "NoneHightlight", false);
				VisualStateManager.GoToState(VerticalNextButton, "NoneHightlight", false);
			}

			if (page == 0)
			{
				//if (ContentColumns.Orientation == Orientation.Horizontal)
				//{
				//	offsetRatio = ContentScrollViewer.HorizontalOffset / ContentColumns.Margin.Left;
				//}
				//else
				//{
				//	offsetRatio = ContentScrollViewer.VerticalOffset / ContentColumns.Margin.Top;
				//}
				//offsetRatio = Math.Max(1 - offsetRatio, 0);
				//HorizontalNextButton.Width = 30 + 50 * offsetRatio;
				//VerticalNextButton.Height = 30 + 50 * offsetRatio;
				VisualStateManager.GoToState(HorizontalPrevButton, "Highlight", false);
				VisualStateManager.GoToState(VerticalPrevButton, "Highlight", false);
			}
			else
			{
				VisualStateManager.GoToState(HorizontalPrevButton, "NoneHightlight", false);
				VisualStateManager.GoToState(VerticalPrevButton, "NoneHightlight", false);
			}

			if (e.IsIntermediate) return;

			int lineNo = GetCurrentLineNo();
			ViewModel.ReportViewChanged(page, lineNo);
			RequestCommentsInView();

		}
		public Task<Chapter> LoadingAheadTask { get; set; }

		public int TotalPage
		{
			get
			{
				return ContentColumns.Children.Count;
			}

		}
		private int GetCurrentPageNo()
		{
			int page = 0;
			if (ContentColumns.Orientation == Orientation.Horizontal)
				page = (int)((ContentScrollViewer.HorizontalOffset + 1 - ContentColumns.Margin.Left) / ContentColumns.ColumnWidth);
			else
				page = (int)((ContentScrollViewer.VerticalOffset + 1 - ContentColumns.Margin.Top) / ContentColumns.ColumnHeight);

			page = Math.Min(Math.Max(0, page), ContentColumns.Children.Count - 1);
			return page;
		}

		private int GetPageNoFromLineNo(int lineNo)
		{
			if (!ContentColumns.IsLayoutValiad || ContentColumns.Children.Count <= 0)
				return -1;
			if (ContentTextBlock.Blocks.Count <= lineNo)
				return -1;
			int idx = ContentTextBlock.Blocks[lineNo].ContentStart.Offset;
			int page = 0;
			if (idx > ContentTextBlock.ContentEnd.Offset)
			{
				page++;
				while (((RichTextBlockOverflow)ContentColumns.Children[page]).ContentEnd.Offset < idx)
					page++;
			}
			return page;
		}

		private int GetCurrentLineNo()
		{
			int page = GetCurrentPageNo();
			if (page >= 1)
			{
				var column = (RichTextBlockOverflow)ContentColumns.Children[page];
				if (column.ContentStart == null)
					return -1;
				var idx = column.ContentStart.Offset;

				// Should use binary search
				//int lineNo = 0;
				//while (ContentTextBlock.Blocks[lineNo].ContentEnd.Offset < idx)
				//	lineNo++;

				int a = 0, b = ContentTextBlock.Blocks.Count - 1, mid = (a + b) / 2;
				while (b > a)
				{
					mid = (a + b) / 2;
					if (ContentTextBlock.Blocks[mid].ContentEnd.Offset < idx)
						a = mid + 1;
					else if (ContentTextBlock.Blocks[mid].ContentStart.Offset > idx)
						b = mid - 1;
					else
						return mid;
				}

				return mid;
			}
			else
			{
				return 0;
			}
		}

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

		private void SyncIndexSelection()
		{
			if (!ViewModel.IsLoading)
			{
				if (VolumeListView.SelectedIndex != ViewModel.VolumeNo)
					VolumeListView.SelectedIndex = ViewModel.VolumeNo;
				if (ChapterListView.SelectedIndex != ViewModel.ChapterNo)
					ChapterListView.SelectedIndex = ViewModel.ChapterNo;
				VolumeListView.ScrollIntoView(VolumeListView.SelectedItem, ScrollIntoViewAlignment.Leading);
				ChapterListView.ScrollIntoView(ChapterListView.SelectedItem, ScrollIntoViewAlignment.Leading);
			}
		}

		//private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		//{
		//	var pageBox = sender as TextBox;
		//	int page = (int)((ContentScrollViewer.HorizontalOffset + 0.1) / ContentTextBlock.Width);
		//	try
		//	{
		//		int newPage = int.Parse(pageBox.Text);
		//		ViewModel.PageNo = newPage;
		//	}
		//	catch (Exception)
		//	{
		//		ViewModel.PageNo = page;
		//		return;
		//	}
		//}


		private void PageBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			int result;
			var pageBox = sender as TextBox;
			if (!((e.Key <= VirtualKey.Number9 && e.Key >= VirtualKey.Number0) || (e.Key <= VirtualKey.NumberPad9 && e.Key >= VirtualKey.NumberPad0)))
			{
				e.Handled = true;
				return;
			}
			var succ = int.TryParse(pageBox.Text, out result);
			if (!succ)
			{
				e.Handled = true;
			}
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				BindingExpression be = pageBox.GetBindingExpression(TextBox.TextProperty);
				if (be != null)
				{
					e.Handled = true;
					DisableAnimationScrollingFlag = false;
					be.UpdateSource();
				}
			}
		}

		private void pageRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (e.NewSize.Width <= 733)
			{
				IndexRegion.Width = e.NewSize.Width;
			}
			else
			{
				IndexRegion.Width = Math.Min(e.NewSize.Width / 2, 640);
			}
			this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
		}

		private async void VolumeListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var list = (ListView)sender;
			if (e.ClickedItem != list.SelectedItem && !ViewModel.IsLoading)
			{
				list.SelectedItem = e.ClickedItem;
				await ViewModel.LoadDataAsync(null, list.SelectedIndex, 0, 0);
				NotifyPropertyChanged("IsFavored");

			}
		}
		private async void ChapterListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var list = (ListView)sender;
			if (e.ClickedItem != list.SelectedItem && !ViewModel.IsLoading)
			{
				list.SelectedItem = e.ClickedItem;
				await ViewModel.LoadDataAsync(null, null, list.SelectedIndex, 0);
				if (UsingLogicalIndexPage)
					IsIndexPanelOpen = false;

			}
			else if (UsingLogicalIndexPage || e.ClickedItem == list.SelectedItem)
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


		public bool DisableAnimationScrollingFlag { get; set; }

		private async void IllustrationSaveButton_Click(object sender, RoutedEventArgs e)
		{
			MessageDialog diag = new MessageDialog("Your image have saved successfully");

			try
			{
				var fileSavePicker = new FileSavePicker();
				var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
				var uri = new Uri(lvm.Content);

				//ScrollViewer sv = (ScrollViewer)(((Grid)((FrameworkElement)((FrameworkElement)sender).Parent).Parent).Children[0]);
				//Image img = sv.Content as Image;
				//var uri = (img.Source as BitmapImage).UriSource;

				var downloadTask = HttpWebRequest.CreateHttp(uri).GetResponseAsync();
				fileSavePicker.FileTypeChoices.Add(".jpg Image", new List<string> { ".jpg" });
				fileSavePicker.DefaultFileExtension = ".jpg";
				fileSavePicker.SuggestedFileName = System.IO.Path.GetFileName(uri.LocalPath);
				var target = await fileSavePicker.PickSaveFileAsync();
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

		private void IndexRegion_LostFocus(object sender, RoutedEventArgs e)
		{
			//if (IndexOpened)
			//	IndexOpened = false;
		}

		private void ContentRegion_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (IsIndexPanelOpen)
			{
				IsIndexPanelOpen = false;
				e.Handled = true;
			}
			else
				e.Handled = false;
		}

		private void FontSizeButton_Click(object sender, RoutedEventArgs e)
		{
			var item = sender as MenuFlyoutItem;
			ViewModel.FontSize = item.FontSize;
			ContentTextBlock.FontWeight = item.FontWeight;
			// Request an update of the layout since the font size is changed
			RichTextColumns.ResetOverflowLayout(ContentColumns, null);
			//ContentColumns.InvalidateMeasure();
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
			//App.Current.Resources["AppReadingBackgroundBrush"] = item.Background;
			//App.Current.Resources["AppBackgroundBrush"] = ViewModel.Background;
			//App.Current.Resources["AppForegroundBrush"] = ViewModel.Foreground;
		}

		private void ContentRegion_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if ((e.Key == VirtualKey.Right || e.Key == VirtualKey.Down || e.Key == VirtualKey.PageDown))
			{
				NextChapterButton_Click(this, null);
			}
			else if ((e.Key == VirtualKey.Left || e.Key == VirtualKey.Up|| e.Key == VirtualKey.PageUp))
			{
				PrevChapterButton_Click(this, null);
			}
		}

		private async void ContentTextBlock_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (!ViewModel.EnableComments)
				return;
			var p = e.GetPosition((UIElement)sender);
			TextPointer tp = null;
			if (sender is RichTextBlock)
			{
				var rt = (RichTextBlock)sender;
				tp = rt.GetPositionFromPoint(p);
			}
			else
			{
				var rt = (RichTextBlockOverflow)sender ;
				tp = rt.GetPositionFromPoint(p);
			}
			var element = tp.Parent as TextElement;
			while (element != null && !(element is Paragraph))
			{
				if (element.ContentStart != null
					&& element != element.ElementStart.Parent)
				{
					element = element.ElementStart.Parent as TextElement;
				}
				else
				{
					element = null;
				}
			}
			if (element == null) return;
			var line = (LineViewModel)element.GetValue(ParagrahViewModelProperty);

			if (line.HasNoComment)
			{
				ContentTextBlock.Select(element.ContentStart, element.ContentEnd);
				((FrameworkElement)CommentsLayout.Content).DataContext = line;
				Flyout.ShowAttachedFlyout((FrameworkElement)sender);
			}
			else
			{
				((FrameworkElement)CommentsLayout.Content).DataContext = line;
				Flyout.ShowAttachedFlyout((FrameworkElement)sender);
				//CommentsLayout.ShowAt((FrameworkElement)sender);
				await line.LoadCommentsAsync();
			}
		}

		private async void CommentSubmitButton_Click(object sender, RoutedEventArgs e)
		{
			var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
			var commentText = CommentInputBox.Text;
			CommentInputBox.Text = string.Empty;
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
			progressBar.Value = e.Progress;
			if (e.Progress == 100)
			{
				progressBar.GetVisualParent().Visibility = Visibility.Collapsed;
			}
		}
	}
}