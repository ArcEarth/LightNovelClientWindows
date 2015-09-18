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
using Windows.ApplicationModel.Core;
//using System.Runtime.InteropServices.WindowsRuntime;
//using System.Windows.Input;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
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
            if (this.RequestedTheme != AppGlobal.Settings.BackgroundTheme)
            {
                this.RequestedTheme = AppGlobal.Settings.BackgroundTheme;
            }
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            this.navigationHelper.GoBackCommand = new LightNovel.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
            RegisterForShare();
            ScrollSwitch = false;
            DisableAnimationScrollingFlag = true;
            ContentRegion.SizeChanged += ContentRegion_SizeChanged;
            //IndexRegion.SizeChanged += IndexRegion_SizeChanged;
            ContentColumns.ColumnsChanged += ContentColumns_LayoutUpdated;
            ContentScrollViewer.LayoutUpdated += ScrollToPage_ContentColumns_LayoutUpdated;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.CommentsListLoaded += ViewModel_CommentsListLoaded;
            Flyout.SetAttachedFlyout(this, ImagePreviewFlyout);
            RefreshThemeColor();
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            ImageLoadingTextPlaceholder = resourceLoader.GetString("ImageLoadingPlaceholderText");
            ImageTapToLoadPlaceholder = resourceLoader.GetString("ImageTapToLoadPlaceholderText");

        }

        //private void IndexRegion_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //	if (e.NewSize.Width >= 500)
        //	{
        //		VolumeListView.Visibility = Visibility.Visible;
        //		ChapterListView.Visibility = Visibility.Visible;
        //		ChapterGroupListView.Visibility = Visibility.Collapsed;
        //	}
        //	else
        //	{
        //		VolumeListView.Visibility = Visibility.Collapsed;
        //		ChapterListView.Visibility = Visibility.Collapsed;
        //		ChapterGroupListView.Visibility = Visibility.Visible;
        //	}

        //}

        void ViewModel_CommentsListLoaded(object sender, IEnumerable<int> e)
        {
            foreach (int idx in e)
            {
                var para = ContentTextBlock.Blocks[idx - 1];
                //var fcolor = (ViewModel.Foreground as SolidColorBrush).Color;
                if (ViewModel.ChapterData.Lines[idx - 1].ContentType == LineContentType.TextContent)
                {
                    var par = para as Paragraph;
                    par.Inlines[0].Foreground = (SolidColorBrush)App.Current.Resources["AppAccentBrush"];
                }
                //para.Foreground = CommentedTextBrush; //(SolidColorBrush)App.Current.Resources["AppAccentBrushLight"];
            }
            //await RequestCommentsInViewAsync();
        }

        void ContentListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int index = (int)ContentListView.GetValue(ListViewScrollToIndexRequestProperty);
            if (index >= 0 && e.NewSize.Height > 10 && ContentListView.Items.Count > 0 && index < ContentListView.Items.Count)
            {
                ContentListView.UpdateLayout();
                ContentListView.ScrollIntoView(ViewModel.Contents[index], ScrollIntoViewAlignment.Leading);
                index = -1;
                ContentListView.SizeChanged -= ContentListView_SizeChanged;
            }
        }

        void ChangeView(int page, int line = -1)
        {
            if (_currentViewOrientation == Orientation.Horizontal)
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
                }
                else if (line >= 0)
                {
                    RequestDelayedChangeView(-1, line);
                }
            }
            else
            {
                if (line >= 0)
                {
                    ContentListView.UpdateLayout();
                    if (ContentListView.Items.Count == 0)
                    {
                        ContentListView.SetValue(ListViewScrollToIndexRequestProperty, line);
                        ContentListView.SizeChanged += ContentListView_SizeChanged;
                    }
                    else if (line < ContentListView.Items.Count)
                    {
                        ContentListView.ScrollIntoView(ViewModel.Contents[line], ScrollIntoViewAlignment.Leading);
                    }
                }
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
            //IndexCollapsedToExpandedKeyFrame.Value = -IndexRegion.Width;
            //ContentCollapsedToExpandedKeyFrame.Value = -IndexRegion.Width;
            //IndexExpandedToCollapsedKeyFrame.Value = -IndexRegion.Width;
            //ContentExpandedToCollapsedKeyFrame.Value = -IndexRegion.Width;
            VisualStateManager.GoToState(this, stateName, useTransitions);
            this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
        }

        void ChangeContentFlowDirection(Orientation orientation)
        {
            if (orientation == Orientation.Vertical)
            {
                ContentColumns.Orientation = Orientation.Vertical;
                ContentColumns.Margin = new Thickness(0, 0, 0, 0);
                ContentTextBlock.Padding = new Thickness(20, 0, 20, 0);
                foreach (UIElement elem in ContentColumns.Children)
                {
                    var rtbo = elem as RichTextBlockOverflow;
                    if (rtbo != null)
                        rtbo.Padding = ContentTextBlock.Padding;
                }
                ContentScrollViewer.Style = (Style)App.Current.Resources["VerticalScrollViewerStyle"];
                ContentScrollViewer.HorizontalSnapPointsType = SnapPointsType.None;
                ContentScrollViewer.VerticalSnapPointsType = SnapPointsType.Optional;
            }
            else
            {
                ContentColumns.Orientation = Orientation.Horizontal;
                ContentColumns.Margin = new Thickness(100, 0, 100, 0);
                ContentTextBlock.Padding = new Thickness(40);
                foreach (UIElement elem in ContentColumns.Children)
                {
                    var rtbo = elem as RichTextBlockOverflow;
                    if (rtbo != null)
                        rtbo.Padding = ContentTextBlock.Padding;
                }
                ContentScrollViewer.Style = (Style)App.Current.Resources["HorizontalScrollViewerStyle"];
                ContentScrollViewer.HorizontalSnapPointsType = SnapPointsType.Mandatory;
                ContentScrollViewer.VerticalSnapPointsType = SnapPointsType.None;
            }
        }

        Orientation _currentViewOrientation;

        Orientation ViewOrientation
        {
            get
            {
                var appView = ApplicationView.GetForCurrentView();
                var bounds = appView.VisibleBounds;
                if (appView.Orientation == ApplicationViewOrientation.Portrait || bounds.Width < 800)
                    return Orientation.Vertical;
                else
                    return Orientation.Horizontal;
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
            if (displaySize.Width <= displaySize.Height * 1.3)
                ColumnsPerScreen = 1;
            else
                ColumnsPerScreen = 2;

            if (displaySize.Width < 200)
            {
                ColumnsPerScreen = 0;
            }

            if (displaySize.Width < 500)
            {
                foreach (var col in ContentColumns.Children)
                {
                    var page = col as RichTextBlockOverflow;
                    if (page != null)
                        page.Padding = ContentTextBlock.Padding;
                }
            }
            else
            {
                foreach (var col in ContentColumns.Children)
                {
                    var page = col as RichTextBlockOverflow;
                    if (page != null)
                        page.Padding = ContentTextBlock.Padding;
                }
            }

            // If not Visible
            if (ColumnsPerScreen <= 0 || ViewOrientation == Orientation.Vertical)
            {
                ContentListView.Visibility = Visibility.Visible;

                ContentColumns.Visibility = Visibility.Collapsed;
                HorizontalPrevButton.Visibility = Visibility.Collapsed;
                HorizontalNextButton.Visibility = Visibility.Collapsed;
                PageIndicator.Visibility = Visibility.Collapsed;

                _currentViewOrientation = Orientation.Vertical;
                //Debug.WriteLine("Content Columns Width is less than 200, this should be impossible to happen in reality use.");
            }
            else
            {
                _currentViewOrientation = Orientation.Horizontal;

                ContentListView.Visibility = Visibility.Collapsed;

                ContentColumns.Visibility = Visibility.Visible;
                HorizontalPrevButton.Visibility = Visibility.Visible;
                HorizontalNextButton.Visibility = Visibility.Visible;
                PageIndicator.Visibility = Visibility.Visible;

                ContentColumns.ColumnWidth = displaySize.Width / ColumnsPerScreen;
                ContentColumns.ColumnHeight = displaySize.Height;
                if (ContentColumns.Visibility != Visibility.Collapsed)
                    ChangeContentFlowDirection(ViewOrientation);
                Relayout_ContentImages();
            }
        }
        private void ContentRegion_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var currentLine = ViewModel.LineNo;

            // Let's see, it should not do things if it's not reasonable size
            if (e.NewSize.Height <= 100 || e.NewSize.Width <= 100)
                return;

            Debug.WriteLine("ContentRegion_SizeChanged, lineNo: {0}", currentLine);
            Relayout_ContentColumn(e.NewSize);
            ChangeView(-1, currentLine);
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
            Uri severBaseUri = LightKindomHtmlClient.SeverBaseUri;
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

                    var illustration = LineViewCCGTemplate.LoadContent() as Grid;
                    illustration.DataContext = line;
                    illustration.Width = ContentColumns.ColumnWidth - padding.Width - 1;
                    illustration.Height = ContentColumns.ColumnHeight - padding.Height - PictureMargin;
                    LoadItemIllustation(illustration, line);
                    (illustration.FindName("ImageContent") as Image).SizeChanged += Image_SizeChanged;
                    //var bitmap = (illustration.GetFirstDescendantOfType<Image>().Source as BitmapImage);
                    //var pb = illustration.GetFirstDescendantOfType<ProgressBar>();
                    //bitmap.SetValue(BitmapLoadingIndicatorProperty, pb);

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
            if (_currentViewOrientation == Orientation.Vertical)
                return;

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
            if (_currentViewOrientation == Orientation.Horizontal)
            {
                int page = GetCurrentPageNo();
                if (page >= 1)
                {
                    var column = (RichTextBlockOverflow)ContentColumns.Children[page];
                    if (column.ContentStart == null)
                        return -1;
                    //var tp = column.GetPositionFromPoint(new Point { X = column.ActualWidth * 0.5, Y = column.ActualHeight * 0.5 });
                    var tp = column.GetPositionFromPoint(new Point { X = 1, Y = 1 });
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
            else
            {
                return (ContentListView.ItemsPanelRoot as ItemsStackPanel).FirstVisibleIndex;
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
            //if (e.NewSize.Width <= 733)
            //{
            //	IndexRegion.Width = e.NewSize.Width;
            //}
            //else
            //{
            //	IndexRegion.Width = Math.Min(e.NewSize.Width / 2, 640);
            //}
            this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
        }

        public bool DisableAnimationScrollingFlag { get; set; }

        private void ContentRegion_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if ((e.Key == VirtualKey.Right || e.Key == VirtualKey.Down || e.Key == VirtualKey.PageDown))
            {
                NextChapterButton_Click(this, null);
            }
            else if ((e.Key == VirtualKey.Left || e.Key == VirtualKey.Up || e.Key == VirtualKey.PageUp))
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
                var rt = (RichTextBlockOverflow)sender;
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
            if (!(line.IsImage || ViewModel.EnableComments))
                return;

            if (line.IsImage)
            {
                var para = element as Paragraph;
                var container = para?.Inlines.OfType<InlineUIContainer>().First();
                var iv = container.Child as Grid;
                SetUpComentFlyoutForLineView(iv,line);

                CommentsFlyout.ShowAt(iv);
            }
            else
            {
                ((FrameworkElement)CommentsFlyout.Content).DataContext = line;

                if (ViewModel.EnableComments)
                {
                    CommentsTool.Visibility = Visibility.Visible;
                    if (AppGlobal.IsSignedIn)
                    {
                        CommentsInputTool.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    else
                    {
                        CommentsInputTool.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }
                }
                else
                {
                    CommentsTool.Visibility = Visibility.Collapsed;
                }
                Flyout.ShowAttachedFlyout((FrameworkElement)sender);

            }


            if (line.HasComments && !line.IsLoading)
            {
                await line.LoadCommentsAsync();
            }

        }

        private void SyncIndexCascadeViewSelection()
        {
            if (ViewModel.VolumeNo < 0 || ViewModel.ChapterNo < 0 || ViewModel.Index.Count <= ViewModel.VolumeNo || ViewModel.Index[ViewModel.VolumeNo].Count <= ViewModel.ChapterNo) return;
            var target = ViewModel.Index[ViewModel.VolumeNo];
            var chpTarget = target[ViewModel.ChapterNo];
            if (ViewModel.HasNext)
            {
                if (ViewModel.ChapterNo + 1 < target.Count)
                {
                    var nextvm = ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo + 1];
                    nextvm.NotifyPropertyChanged("IsDownloaded");
                }
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
        private void SyncIndexSelection()
        {
            SyncIndexCascadeViewSelection();
            SyncIndexGroupViewSelection();
        }
        private void SyncIndexGroupViewSelection()
        {
            if (ViewModel.VolumeNo < 0 || ViewModel.ChapterNo < 0 || ViewModel.Index.Count <= ViewModel.VolumeNo || ViewModel.Index[ViewModel.VolumeNo].Count <= ViewModel.ChapterNo) return;
            var target = ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo];
            if (target != null)
                target.NotifyPropertyChanged("IsDownloaded");
            if (ViewModel.HasNext)
            {
                ChapterPreviewModel nextvm;
                if (ViewModel.ChapterNo + 1 < ViewModel.Index[ViewModel.VolumeNo].Count)
                {
                    nextvm = ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo + 1];
                }
                else
                {
                    nextvm = ViewModel.Index[ViewModel.VolumeNo + 1][0];
                }
                nextvm.NotifyPropertyChanged("IsDownloaded");
            }
            if (ChapterGroupListView.ActualHeight > 0 && ChapterGroupListView.Items.Count > 0)
            {
                ChapterGroupListView.SelectedItem = target;
                ChapterGroupListView.UpdateLayout();
                ChapterGroupListView.ScrollIntoView(target, ScrollIntoViewAlignment.Leading);
            }
            else
            {
                if (ChapterGroupListView.GetValue(ListViewBaseScrollToItemRequestProperty) == null)
                {
                    ChapterGroupListView.SetValue(ListViewBaseScrollToItemRequestProperty, target);
                    ChapterGroupListView.SizeChanged += ListView_SizeChanged_ScrollToItem;
                }
                else
                {
                    ChapterGroupListView.SetValue(ListViewBaseScrollToItemRequestProperty, target);
                }
            }
            //var scrollViewer = ChapterGroupListView.GetScrollViewer();
            //if (scrollViewer != null)
            //    scrollViewer.ChangeView(null, scrollViewer.VerticalOffset + 100, null, true);
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

        private async void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            image.ImageOpened -= Image_ImageOpened;
            //RelayoutImageCommentsIndicator(image);
            await image.FadeInCustom(new TimeSpan(0, 0, 0, 0, 500), null, 1);

        }

        private static void RelayoutImageCommentsIndicator(Image image)
        {
            var parent = image.GetVisualParent();
            var indicator = parent.FindName("CommentIndicator") as Rectangle;
            var rect = image.GetBoundingRect(parent);
            indicator.VerticalAlignment = VerticalAlignment.Top;
            indicator.Height = rect.Height;
            indicator.Margin = new Thickness(rect.Left - 5, rect.Top, 0, 0);
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var image = sender as Image;
            RelayoutImageCommentsIndicator(image);
        }

        private async void ChapterGroupListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ViewModel.IsLoading)
                return;
            var list = (ListView)sender;
            if (e.ClickedItem != list.SelectedItem)
            {
                list.SelectedItem = e.ClickedItem;
                var cpvm = e.ClickedItem as ChapterPreviewModel;
                if (UsingLogicalIndexPage)
                    IsIndexPanelOpen = false;
                if (cpvm.VolumeNo < ViewModel.VolumeNo || (cpvm.VolumeNo == ViewModel.VolumeNo && cpvm.No < ViewModel.ChapterNo))
                    TranslationType = -1;
                else
                    TranslationType = 1;
                await ViewModel.LoadDataAsync(-1, cpvm.VolumeNo, cpvm.No, 0);
            }
            else if (UsingLogicalIndexPage || e.ClickedItem == list.SelectedItem)
            {
                IsIndexPanelOpen = false;
            }
        }

        private async void OpenInNewViewButton_Click(object sender, RoutedEventArgs e)
        {
            var bookmark = ViewModel.CreateBookmark();
            var view = await CreateInNewViewAsync(ViewModel.SeriesData.Title, bookmark.Position);
            if (view != null)
            {

                var shown = await view.ShowAsync();

                if (shown && this.Frame.CanGoBack)
                {
                    DisableUpdateOnNavigateFrom = true;
                    this.Frame.GoBack();
                    this.Frame.ForwardStack.RemoveAt(this.Frame.ForwardStack.Count - 1);
                }
            }

        }

        public static async Task<ViewLifetimeControl> CreateInNewViewAsync(string seriesTitle, NovelPositionIdentifier nav)
        {
            ViewLifetimeControl viewControl = null;

            await CoreApplication.CreateNewView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                // This object is used to keep track of the views and important
                // details about the contents of those views across threads
                // In your app, you would probably want to track information
                // like the open document or page inside that window

                viewControl = ViewLifetimeControl.CreateForCurrentView();
                viewControl.Title = seriesTitle;

                AppGlobal.SecondaryViews.Add(viewControl);

                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;

                var appView = ApplicationView.GetForCurrentView();
                appView.Title = viewControl.Title;
                appView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);

                var titleBar = appView.TitleBar;
                titleBar.BackgroundColor = Colors.Transparent;
                titleBar.ButtonBackgroundColor = (Color)App.Current.Resources["AppAccentColor"];
                titleBar.ButtonInactiveBackgroundColor = (Color)App.Current.Resources["AppAccentColor"];

                await Task.Delay(TimeSpan.FromMilliseconds(200));

                var frame = new Frame();
                Window.Current.Content = frame;
                frame.Navigate(typeof(ReadingPage), nav.ToString());
                var readingPage = frame.Content as ReadingPage;

                if (readingPage != null)
                    viewControl.Released += readingPage.ViewControl_Released;


                //ApplicationView.GetForCurrentView().Consolidated += readingPage.ReadingPage_Consolidated;
                //viewControl.StartViewInUse();
            });

            return viewControl;
        }

        private void ReadingPage_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
        }

        private async void ViewControl_Released(object sender, EventArgs e)
        {
            var thisViewControl = ((ViewLifetimeControl)sender);
            thisViewControl.Released -= ViewControl_Released;
            // The ViewLifetimeControl object is bound to UI elements on the main thread
            // So, the object must be removed from that thread
            await App.Current.MainDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
             {
                 AppGlobal.SecondaryViews.Remove(thisViewControl);
                 await UpdateApplicationHistory();
             });

            // The released event is fired on the thread of the window
            // it pertains to.
            //
            // It's important to make sure no work is scheduled on this thread
            // after it starts to close (no data binding changes, no changes to
            // XAML, creating new objects in destructors, etc.) since
            // that will throw exceptions
            Window.Current.Close();
        }

        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsFullScreen = !ViewModel.IsFullScreen;
        }

        private async void RefreshContentButtonClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.RefreshSeriesDataAsync();
        }
        private void SlideHandle_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.Cumulative.Translation.X >= 25)
            {
                e.Complete();
                IsIndexPanelOpen = !IsIndexPanelOpen;
            }
        }

        private void IndexPanel_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.Cumulative.Translation.X <= -25)
            {
                e.Complete();
                IsIndexPanelOpen = false;
            }
        }

        private void HideImageRefreshButton()
        {
            ImageRefreshColumn.Width = new GridLength(0, GridUnitType.Star);
            ImageRefreshButton.IsEnabled = false;
            ImageRefreshButton.Visibility = Visibility.Collapsed;
        }

        private void ShowImageRefreshButton()
        {
            ImageRefreshColumn.Width = new GridLength(1, GridUnitType.Star);
            ImageRefreshButton.IsEnabled = true;
            ImageRefreshButton.Visibility = Visibility.Visible;
        }

        private void ContentListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            var iv = args.ItemContainer.ContentTemplateRoot as Grid;
            if (args.InRecycleQueue)
            {
                return;
                var imageContent = iv.FindName("ImageContent") as Image;
                var textContent = iv.FindName("TextContent") as TextBlock;
                var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
                var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;
                progressIndicator.Value = 0;
                if (imageContent.Source != null)
                {
                    var bitmap = imageContent.Source as BitmapImage;
                    bitmap.DownloadProgress -= Image_DownloadProgress;
                }
                imageContent.DataContext = null;
                imageContent.ClearValue(Image.SourceProperty);
                imageContent.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                imageContent.Height = 0;
                textContent.ClearValue(TextBlock.TextProperty);
                iv.Height = double.NaN;
                args.ItemContainer.InvalidateMeasure();
                args.Handled = true;
                return;
            }

            if (args.Phase == 0)
            {
                var imageContent = iv.FindName("ImageContent") as Image;
                var textContent = iv.FindName("TextContent") as TextBlock;
                var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;
                var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
                if (!Double.IsNaN(sender.ActualWidth) && sender.ActualWidth > 0)
                    iv.MaxWidth = sender.ActualWidth;

                var imagePlaceHolder = iv.FindName("ImagePlaceHolder") as Windows.UI.Xaml.Shapes.Path;

                var line = (LineViewModel)args.Item;

                iv.Height = double.NaN;
                imageContent.Opacity = 0;
                imageContent.Height = double.NaN;
                commentIndicator.Opacity = 0;
                progressIndicator.Opacity = 0;
                textContent.Opacity = 1;

                if (imageContent.Source != null)
                {
                    var bitmap = imageContent.Source as BitmapImage;
                    bitmap.DownloadProgress -= Image_DownloadProgress;
                    imageContent.ClearValue(Image.SourceProperty);
                }

                if (line.IsImage)
                {
                    progressIndicator.Visibility = Visibility.Visible;
                    if (!AppGlobal.ShouldAutoLoadImage)
                    {
                        textContent.Text = ImageTapToLoadPlaceholder;
                        //imageContent.MinHeight = 440;
                        imageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        imagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        textContent.TextAlignment = TextAlignment.Center;
                    }
                    else
                    {
                        textContent.Text = ImageLoadingTextPlaceholder;
                        //imageContent.MinHeight = 440;
                        imagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        imageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        textContent.TextAlignment = TextAlignment.Center;
                    }
                }
                else
                {
                    textContent.Text = "　" + line.Content;
                    //textContent.Height = double.NaN;
                    textContent.TextAlignment = TextAlignment.Left;

                    imagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    imageContent.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    imageContent.DataContext = null;
                }

                args.RegisterUpdateCallback(ContentListView_ContainerContentChanging);
                //} else if (args.Phase == 1)
                //{
                //	//var textContent = iv.FindName("TextContent") as TextBlock;
                //	//textContent.Opacity = 1;
                //	//args.RegisterUpdateCallback(ContentListView_ContainerContentChanging); 
            }
            else if (args.Phase == 1) // Show comment indicator rectangle / progress bar
            {
                var line = (LineViewModel)args.Item;
                var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;
                if (line.HasComments)
                {
                    commentIndicator.Opacity = 1;
                    args.RegisterUpdateCallback(3, ContentListView_ContainerContentChanging);
                }
                if (line.IsImage)
                {
                    var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
                    progressIndicator.Value = 0;
                    progressIndicator.Opacity = 1;
                    if (AppGlobal.ShouldAutoLoadImage)
                        args.RegisterUpdateCallback(2, ContentListView_ContainerContentChanging);
                }
            }
            else if (args.Phase == 2)
            {
                var line = (LineViewModel)args.Item;
                LoadItemIllustation(iv, line);
            }
            else if (args.Phase == 3)
            {
                var line = (LineViewModel)args.Item;
                line.LoadCommentsAsync();
            }
            args.Handled = true;
        }

        private void LoadItemIllustation(Grid iv, LineViewModel line)
        {
            var bitMap = new BitmapImage(line.ImageUri);
            var imageContent = iv.FindName("ImageContent") as Image;
            imageContent.DataContext = line;
            var imagePlaceHolder = iv.FindName("ImagePlaceHolder") as Windows.UI.Xaml.Shapes.Path;
            var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
            var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;

            commentIndicator.Opacity = line.HasComments ? 1 : 0;

            bitMap.SetValue(BitmapLoadingIndicatorProperty, progressIndicator);
            bitMap.DownloadProgress += Image_DownloadProgress;
            imageContent.ImageOpened += imageContent_ImageOpened;
            imageContent.ImageFailed += ImageContent_Failed;
            imageContent.Source = bitMap;
        }

        async void imageContent_ImageOpened(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            var imagePlaceHolder = image.GetVisualParent().FindName("ImagePlaceHolder") as Windows.UI.Xaml.Shapes.Path;
            image.ImageOpened -= imageContent_ImageOpened;
            imagePlaceHolder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            await image.FadeInCustom(new TimeSpan(0, 0, 0, 0, 500), null, 1);
        }

        private void Image_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            var bitmap = sender as BitmapImage;
            var progressBar = bitmap.GetValue(BitmapLoadingIndicatorProperty) as ProgressBar;
            if (progressBar == null) return;
            progressBar.Value = e.Progress;
            if (e.Progress == 100)
            {
                var iv = progressBar.GetVisualParent();
                if (iv == null) return;
                var textContent = iv.FindName("TextContent") as TextBlock;
                var imageContent = iv.FindName("ImageContent") as Image;
                textContent.Opacity = 0;
                progressBar.Opacity = 0;
                //imageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private async void IllustrationRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
            if (lvm.IsImageCached) // LocalUri
            {
                var container = ContentListView.ContainerFromIndex(lvm.No - 1) as ContentControl;
                if (container != null)
                {
                    var iv = container.ContentTemplateRoot as Grid;
                    HideImageRefreshButton();
                    await RefreshCruptedImageItem(iv);
                }
            }
        }

        void SetUpComentFlyoutForLineView(Grid iv, LineViewModel line)
        {
            var textContent = iv.FindName("TextContent") as TextBlock;
            if (line.IsImage && textContent.Text == ImageTapToLoadPlaceholder)
            {
                textContent.Text = ImageLoadingTextPlaceholder;
                LoadItemIllustation(iv, line);
                return;
            }

            if (!(line.IsImage || ViewModel.EnableComments))
                return;

            ((FrameworkElement)CommentsFlyout.Content).DataContext = line;
            if (ViewModel.EnableComments)
            {
                CommentsTool.Visibility = Visibility.Visible;
                if (AppGlobal.IsSignedIn)
                {
                    CommentsInputTool.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    CommentsInputTool.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                CommentsTool.Visibility = Visibility.Collapsed;
            }

            if (line.IsImage && line.IsImageCached)
            {
                var imageContent = iv.FindName("ImageContent") as Image;
                var bitmap = imageContent.Source as BitmapImage;
                if (bitmap.UriSource.AbsoluteUri.StartsWith("ms-appdata"))
                {
                    ShowImageRefreshButton();
                }
                else
                {
                    HideImageRefreshButton();
                }
            }
            else
            {
                HideImageRefreshButton();
            }
        }

        private async void ContentListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (IsIndexPanelOpen)
            {
                IsIndexPanelOpen = false;
                return;
            }

            var line = (LineViewModel)e.ClickedItem;

            var container = ContentListView.ContainerFromIndex(line.No - 1) as ContentControl;
            var iv = container.ContentTemplateRoot as Grid;

            SetUpComentFlyoutForLineView(iv, line);

            CommentsFlyout.ShowAt((FrameworkElement)container);

            if (line.HasComments && !line.IsLoading)
            {
                await line.LoadCommentsAsync();
            }
        }


        private async void ImageContent_Failed(object sender, ExceptionRoutedEventArgs e)
        {
            var iv = ((Image)sender).GetFirstAncestorOfType<Grid>() as Grid;
            await RefreshCruptedImageItem(iv);
        }


        private async Task RefreshCruptedImageItem(Grid iv)
        {
            var imageContent = iv.FindName("ImageContent") as Image;
            var lvm = imageContent.DataContext as LineViewModel;
            if (lvm == null) return;

            var bitmap = imageContent.Source as BitmapImage;
            if (bitmap == null) return;

            var uri = bitmap.UriSource.AbsoluteUri;
            if (uri.StartsWith("ms-appdata"))
            {

                var textContent = iv.FindName("TextContent") as TextBlock;
                var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;
                var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
                var imagePlaceholder = iv.FindName("ImagePlaceHolder") as Windows.UI.Xaml.Shapes.Path;

                var remoteUri = lvm.Content;

                textContent.Text = ImageLoadingTextPlaceholder;
                imagePlaceholder.Visibility = Windows.UI.Xaml.Visibility.Visible;
                imagePlaceholder.Opacity = 1;
                imageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
                textContent.TextAlignment = TextAlignment.Center;
                textContent.Opacity = 1;
                imageContent.Opacity = 0;
                progressIndicator.Opacity = 1;

                imageContent.ImageOpened -= imageContent_ImageOpened;
                bitmap.DownloadProgress -= Image_DownloadProgress;

                bitmap.UriSource = new Uri(remoteUri);
                bitmap.DownloadProgress += Image_DownloadProgress;
                imageContent.ImageOpened += imageContent_ImageOpened;

                await CachedClient.DeleteIllustationAsync(remoteUri);
            } // else is Network Issue
        }

        private void PageBottomCommandBar_IsOpenChanged(object sender, object e)
        {
            if (!ViewModel.IsFullScreen)
                return;
            SyncBottomAppBarTheme();
            if (PageBottomCommandBar.IsOpen)
            {
                VisualStateManager.GoToState(this, "PeekTitleBarState", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "HideTitleBarState", true);
            }
        }

        private void SyncBottomAppBarTheme()
        {
            if (!ViewModel.IsFullScreen || PageBottomCommandBar.IsOpen)
            {
                PageBottomCommandBar.Background = (SolidColorBrush)App.Current.Resources["AppAccentBrush"];
                PageBottomCommandBar.Foreground = (SolidColorBrush)App.Current.Resources["AppBackgroundBrush"];
            }
            else
            {
                PageBottomCommandBar.Background = new SolidColorBrush(Colors.Transparent);
                PageBottomCommandBar.Foreground = (SolidColorBrush)App.Current.Resources["AppAccentBrush"];
            }
        }

        private void ImageSetCoverButton_Click(object sender, RoutedEventArgs e)
        {
            var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
            ViewModel.SetCustomizeCover(lvm.ImageUri);
        }

        private void PageBottomCommandBar_Opening(object sender, object e)
        {
            if (ViewModel.IsFullScreen)
                VisualStateManager.GoToState(this, "PeekTitleBarState", true);
        }

        private void PageBottomCommandBar_Closing(object sender, object e)
        {
            if (ViewModel.IsFullScreen)
                VisualStateManager.GoToState(this, "HideTitleBarState", true);
        }
    }
}