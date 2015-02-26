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

	/// <summary>
	/// A page that displays details for a single item within a group.
	/// </summary>
	public sealed partial class ReadingPage : Page, INotifyPropertyChanged
	{

		/// <summary>
		/// Identifies the <see cref="ParagrahNoProperty"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty ParagrahViewModelProperty =
			DependencyProperty.Register("ParagrahViewModel", typeof(LineViewModel),
			typeof(Paragraph), new PropertyMetadata(null, null));


		private double PictureMargin = 8;
		private int ColumnsPerScreen = 2;

		private int MinimumWidthForSupportingTwoPanes = 768;
		public bool UsingLogicalIndexPage
		{
			get
			{
				return Window.Current.Bounds.Width < MinimumWidthForSupportingTwoPanes;
			}
		}

		public ReadingPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
			this.navigationHelper.GoBackCommand = new LightNovel.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
			RegisterForShare();
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


		void ViewModel_CommentsListLoaded(object sender, IEnumerable<int> e)
		{
			foreach (int idx in e)
			{
				var para = ContentTextBlock.Blocks[idx-1];
				//var fcolor = (ViewModel.Foreground as SolidColorBrush).Color;
				if (ViewModel.ChapterData.Lines[idx-1].ContentType == LineContentType.TextContent)
				{
					var par = para as Paragraph;
					par.Inlines[0].Foreground = (SolidColorBrush)App.Current.Resources["AppAccentBrush"];
				}
				//para.Foreground = CommentedTextBrush; //(SolidColorBrush)App.Current.Resources["AppAccentBrushLight"];
			}
			//await RequestCommentsInViewAsync();
		}

		void ChangeView(int page, int line = -1)
		{
			if (line >= 0)
				page = GetPageNoFromLineNo(ViewModel.LineNo); // Use Line index if assigned

			if (page >= 0) // Use page index instead of line index
			{
				// Is Current View state fully loaded
				if (ContentColumns.Visibility != Visibility.Visible || !ContentColumns.IsLayoutValiad)
				{
					RequestDelayedChangeView(ViewModel.PageNo);
				}
				else
				{
					ScrollToPage(ViewModel.PageNo, DisableAnimationScrollingFlag);
					DisableAnimationScrollingFlag = true;
				}
			} else if (line >= 0)
			{
				RequestDelayedChangeView(-1,line);
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
			Relayout_ContentColumn(e.NewSize);
			RequestDelayedChangeView(-1,currentLine);
		}

		void ContentColumns_LayoutUpdated(object sender, object e)
		{
			Debug.WriteLine("ContentColumns_LayoutUpdated.");
			ViewModel.PagesCount = ContentColumns.Children.Count;
		}

		public bool ScrollSwitch { get; set; }
		public bool UseTargetLine { get; set; }
		public int TargetLineNo { get; set; }
		public int TargetPageNo { get; set; }

		void RequestDelayedChangeView(int page, int line = -1)
		{
			if (page >= 0)
			{
				ScrollSwitch = true;
				UseTargetLine = false;
				TargetPageNo = page;
			}
			else if (line >= 0)
			{
				ScrollSwitch = true;
				UseTargetLine = true;
				TargetLineNo = line;
			}
		}

		bool ScrollToPage(int page, bool disableAnimation)
		{
			if (ContentScrollViewer.HorizontalScrollMode != ScrollMode.Disabled)
				return ContentScrollViewer.ChangeView(ContentColumns.Margin.Left + page * ContentColumns.ColumnWidth + 0.1, null, null, disableAnimation);
			else
				return ContentScrollViewer.ChangeView(null, ContentColumns.Margin.Top + page * ContentColumns.ColumnHeight + 0.1, null, disableAnimation);
		}

		void ScrollToPage_ContentColumns_LayoutUpdated(object sender, object e)
		{
			//Debug.WriteLine("ScrollViewer_LayoutUpdated , Size ({0},{1})", ContentScrollViewer.ActualWidth,ContentScrollViewer.ActualHeight);
			if (!ScrollSwitch || !ContentColumns.IsLayoutValiad || ContentColumns.Visibility != Windows.UI.Xaml.Visibility.Visible || TotalPage <= 0)
				return;

			var page = TargetPageNo;

			if (UseTargetLine)
				page = GetPageNoFromLineNo(TargetLineNo);

			if (page >= 0)
			{
				ScrollToPage(page, true);
				ScrollSwitch = false;
				Debug.WriteLine("Delayed Scroll Appears!");
			}
			else
			{
				Debug.WriteLine("Delayed Scroll Failed!");
			}

		}

		private FontFamily SegoeUISymbolFontFamily = new FontFamily("Segoe UI Symbol");
		private SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
		private string CommentIndicator = WebUtility.HtmlDecode("&#xE134;　");

		private void UpdateContentsView(IEnumerable<LineViewModel> lines)
		{
			Uri severBaseUri = new Uri("http://lknovel.lightnovel.cn");
			ContentTextBlock.Blocks.Clear();
			bool prevLineBreakFlag = false;
			foreach (var line in lines)
			{
				var para = new Paragraph();
				para.SetValue(ParagrahViewModelProperty, line);
				if (!line.IsImage || line.Content == null)
				{
					//if (line.HasComments)
					//	para.Inlines.Add(new InlineUIContainer
					//	{
					//		Child = new SymbolIcon { Symbol = Symbol.Message },
					//		Foreground = (SolidColorBrush)App.Current.Resources["AppAcentBrush"]
					//	});
					var run = new Run { Text = line.Content };
					para.Inlines.Add(new Run { Text = CommentIndicator, FontFamily = SegoeUISymbolFontFamily, Foreground = TransparentBrush });
					para.Inlines.Add(run);
					//para.TextIndent = ContentTextBlock.FontSize * 1;
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

		private async void PrevChapterButton_Click(object sender, RoutedEventArgs e)
		{
			int page = GetCurrentPageNo();
			if (page == 0)
			{
				if (ViewModel.ChapterNo > 0 && !ViewModel.IsLoading)
				{
					await ViewModel.LoadDataAsync(-1, -1, ViewModel.ChapterNo - 1, -1);
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
					await ViewModel.LoadDataAsync(-1, -1, ViewModel.ChapterNo + 1, 0);
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

		private void ContentScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			int page = GetCurrentPageNo();
			ViewModel.ReportViewChanged(page, null);

			//if (TotalPage - page < 50 && (LoadingAheadTask == null || LoadingAheadTask.IsFaulted) && !string.IsNullOrEmpty(ViewModel.ChapterData.NextChapterId))
			//	LoadingAheadTask = CachedClient.GetChapterAsync(ViewModel.ChapterData.NextChapterId);

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
			//RequestCommentsInView();

		}
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
				var tp = column.GetPositionFromPoint(new Point{X=column.ActualWidth*0.5,Y=column.ActualHeight*0.5});
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
				if (element == null) return -1;

				var line = (LineViewModel)element.GetValue(ParagrahViewModelProperty);

				return line.No - 1;
			}
			else
			{
				return 0;
			}
		}

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

		public bool DisableAnimationScrollingFlag { get; set; }

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
			if (IsIndexPanelOpen)
			{
				IsIndexPanelOpen = false;
				e.Handled = true;
				return;
			}

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

			if (!line.IsImage && !ViewModel.EnableComments)
					return;

			if (line.HasNoComment)
			{
				//ContentTextBlock.Select(element.ContentStart, element.ContentEnd);
				if (this.RequestedTheme == ElementTheme.Light)
					((FrameworkElement)CommentsFlyout.Content).RequestedTheme = ElementTheme.Light;
				else
					((FrameworkElement)CommentsFlyout.Content).RequestedTheme = ElementTheme.Dark;
				((FrameworkElement)CommentsFlyout.Content).DataContext = line;
				Flyout.ShowAttachedFlyout((FrameworkElement)sender);
			}
			else
			{
				if (this.RequestedTheme == ElementTheme.Light)
					((FrameworkElement)CommentsFlyout.Content).RequestedTheme = ElementTheme.Light;
				else
					((FrameworkElement)CommentsFlyout.Content).RequestedTheme = ElementTheme.Dark;
				((FrameworkElement)CommentsFlyout.Content).DataContext = line;
				Flyout.ShowAttachedFlyout((FrameworkElement)sender);
				await line.LoadCommentsAsync();
			}
		}

		//private void SyncIndexSelection()
		//{
		//	if (!ViewModel.IsLoading)
		//	{
		//		if (VolumeListView.SelectedIndex != ViewModel.VolumeNo)
		//			VolumeListView.SelectedIndex = ViewModel.VolumeNo;
		//		if (ChapterListView.SelectedIndex != ViewModel.ChapterNo)
		//			ChapterListView.SelectedIndex = ViewModel.ChapterNo;
		//		VolumeListView.ScrollIntoView(VolumeListView.SelectedItem, ScrollIntoViewAlignment.Leading);
		//		ChapterListView.ScrollIntoView(ChapterListView.SelectedItem, ScrollIntoViewAlignment.Leading);
		//	}
		//}
		private void SyncIndexSelection()
		{
			if (ViewModel.VolumeNo < 0 || ViewModel.ChapterNo < 0 || ViewModel.Index.Count <= ViewModel.VolumeNo || ViewModel.Index[ViewModel.VolumeNo].Count <= ViewModel.ChapterNo) return;
			var target = ViewModel.Index[ViewModel.VolumeNo];
			var chpTarget = target[ViewModel.ChapterNo];
			if (ViewModel.HasNext)
			{
				var nextvm = ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo + 1];
				nextvm.NotifyPropertyChanged("IsDownloaded");
			}
			if (VolumeListView.ActualHeight > 0 && VolumeListView.Items.Count > 0)
			{
				VolumeListView.SelectedItem = target;
				VolumeListView.UpdateLayout();
				VolumeListView.ScrollIntoView(target, ScrollIntoViewAlignment.Leading);
			}
			else
			{
				if (VolumeListView.GetValue(ListViewBaseScrollToItemRequestProperty) == null)
				{
					VolumeListView.SetValue(ListViewBaseScrollToItemRequestProperty, target);
					VolumeListView.SizeChanged += ListView_SizeChanged_ScrollToItem;
				}
				else
				{
					VolumeListView.SetValue(ListViewBaseScrollToItemRequestProperty, target);
				}
			}

			if (ChapterListView.ActualHeight > 0 && ChapterListView.Items.Count > 0)
			{
				ChapterListView.SelectedItem = chpTarget;
				ChapterListView.UpdateLayout();
				ChapterListView.ScrollIntoView(chpTarget, ScrollIntoViewAlignment.Leading);
			}
			else
			{
				if (ChapterListView.GetValue(ListViewBaseScrollToItemRequestProperty) == null)
				{
					ChapterListView.SetValue(ListViewBaseScrollToItemRequestProperty, chpTarget);
					ChapterListView.SizeChanged += ListView_SizeChanged_ScrollToItem;
				}
				else
				{
					ChapterListView.SetValue(ListViewBaseScrollToItemRequestProperty, chpTarget);
				}
			}
		}

		private async void VolumeListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var list = (ListView)sender;
			if (e.ClickedItem != list.SelectedItem && !ViewModel.IsLoading)
			{
				list.SelectedItem = e.ClickedItem;
				await ViewModel.LoadDataAsync(-1, list.SelectedIndex, 0, 0);
				NotifyPropertyChanged("IsFavored");

			}
		}

		private async void ChapterListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var list = (ListView)sender;
			if (e.ClickedItem != list.SelectedItem && !ViewModel.IsLoading)
			{
				list.SelectedItem = e.ClickedItem;
				var cpvm = e.ClickedItem as ChapterPreviewModel;
				if (UsingLogicalIndexPage)
					IsIndexPanelOpen = false;
				await ViewModel.LoadDataAsync(-1, cpvm.VolumeNo, cpvm.No, 0);
			}
			else if (UsingLogicalIndexPage || e.ClickedItem == list.SelectedItem)
			{
				IsIndexPanelOpen = false;
			}
		}

	}
}