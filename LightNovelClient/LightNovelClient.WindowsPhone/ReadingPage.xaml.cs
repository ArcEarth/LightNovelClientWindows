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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using WinRTXamlToolkit.Controls.Extensions;
using WinRTXamlToolkit.AwaitableUI;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Animation;
//using PostSharp.Patterns.Model;

// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace LightNovel
{
	public class TemplateSelectorContent : ContentControl
	{
		public DataTemplateSelector CententTemplateSelector { get; set; }

		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);
			if (CententTemplateSelector != null)
				ContentTemplate = CententTemplateSelector.SelectTemplate(newContent, this);
		}
	}


	public class NovelLineDataTemplateSelector : DataTemplateSelector
	{
		public DataTemplate IllustrationDataTemplate { get; set; }
		public DataTemplate TextDataTemplate { get; set; }
		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			//FrameworkElement element = container as FrameworkElement;
			if (item != null && item is LineViewModel)
			{
				LineViewModel line = item as LineViewModel;

				if (line.IsImage)
					return IllustrationDataTemplate;
				else
					return TextDataTemplate;
			}

			return base.SelectTemplate(item, container);
		}
	}


	public sealed partial class ReadingPage : Page
	{
		public ReadingPage()
		{
			//DisplayInformation.GetForCurrentView().OrientationChanged += DisplayProperties_OrientationChanged;
			this.InitializeComponent();
			this.SizeChanged += ReadingPage_SizeChanged;
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
			this.navigationHelper.GoBackCommand = new LightNovel.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
			ViewModel.CommentsListLoaded += ViewModel_CommentsListLoaded;
			RegisterForShare();
			Flyout.SetAttachedFlyout(this, ImagePreviewFlyout);
			RefreshThemeColor();
			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			ImageLoadingTextPlaceholder = resourceLoader.GetString("ImageLoadingPlaceholderText");
			ImageTapToLoadPlaceholder = resourceLoader.GetString("ImageTapToLoadPlaceholderText");
		}

		void ViewModel_CommentsListLoaded(object sender, IEnumerable<int> e)
		{
			foreach (int idx in e)
			{
				var container = ContentListView.ContainerFromIndex(idx - 1) as SelectorItem;
				if (container != null)
				{
					var iv = container.ContentTemplateRoot as Grid;
					if (iv == null)
						continue;
					var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;
					commentIndicator.Opacity = 1;
				}
			}
		}

		async void ReadingPage_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			await UpdateSizeOrientationDependentResourcesAsync();
		}

		private async Task UpdateSizeOrientationDependentResourcesAsync()
		{
			//ReadyToLoadingEndingFrame.Value = -ContentRegion.ActualHeight;
			//LoadingToReadyIntialFrame.Value = ContentRegion.ActualHeight;

			var appView = ApplicationView.GetForCurrentView();
			var statusBar = StatusBar.GetForCurrentView();
			if (appView.Orientation == ApplicationViewOrientation.Portrait)
			{
				appView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
				statusBar.ProgressIndicator.Text = " ";
				statusBar.ProgressIndicator.ProgressValue = 0;
				statusBar.ForegroundColor = (Color)App.Current.Resources["AppBackgroundColor"];
				//statusBar.BackgroundColor = (Color)App.Current.Resources["AppAccentColor"];
				//LayoutRoot.Margin = new Thickness(0, 0, 0, 20);
				//ContentListView.Margin = new Thickness(0, 0, 0, 0);
				await statusBar.ShowAsync();
				await statusBar.ProgressIndicator.ShowAsync();
				BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
				AppBarHint.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				BottomAppBar.Closed -= BottomAppBar_Closed;
			}
			else
			{
				appView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
				//ContentListView.Margin = new Thickness(0, 0, 0, -20);
				//LayoutRoot.Margin = new Thickness(0);
				await statusBar.HideAsync();
				BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				BottomAppBar.IsOpen = false;
				BottomAppBar.Closed -= BottomAppBar_Closed;
				BottomAppBar.Closed += BottomAppBar_Closed;
				AppBarHint.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
		}

		void BottomAppBar_Closed(object sender, object e)
		{
			BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
		}

		private bool CanGoBack()
		{
			if (this.UsingLogicalIndexPage && IsIndexPanelOpen)
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
			if (this.UsingLogicalIndexPage && IsIndexPanelOpen)
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


		int GetCurrentLineNo()
		{
			return ContentListView.GetFirstVisibleIndex();
		}


		void ChangeView(int page, int line = -1)
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

		void UpdateContentsView(IEnumerable<LineViewModel> lines)
		{
		}

		public bool UsingLogicalIndexPage { get { return true; } }

		void ChangeState(string stateName, bool useTransitions = true)
		{
			//IndexCollapsedToExpandedKeyFrame.Value = -IndexRegion.Width;
			//ContentCollapsedToExpandedKeyFrame.Value = -IndexRegion.Width;
			//IndexExpandedToCollapsedKeyFrame.Value = -IndexRegion.Width;
			//ContentExpandedToCollapsedKeyFrame.Value = -IndexRegion.Width;
			//IndexPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
			bool result = VisualStateManager.GoToState(this, stateName, useTransitions);
			//this.navigationHelper.GoBackCommand.RaiseCanExecuteChanged();
		}

		private void IllustrationViewGrid_Loaded(object sender, RoutedEventArgs e)
		{
			var img = (sender as Grid).GetFirstDescendantOfType<Image>();
			var indicator = (sender as Grid).GetFirstDescendantOfType<ProgressBar>();
			var bitmap = img.Source as BitmapImage;
			bitmap.SetValue(BitmapLoadingIndicatorProperty, indicator);
		}
		private async void PrevChapterButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.ChapterNo > 0 && !ViewModel.IsLoading)
			{
				TranslationType = -1;
				await ViewModel.LoadDataAsync(-1, -1, ViewModel.ChapterNo - 1, -1, PreCachePolicy.CachePrev);
				//ChangeView(-1, ViewModel.ChapterData.Lines.Count - 2);
			}
		}

		private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.ChapterNo < ViewModel.Index[ViewModel.VolumeNo].Chapters.Count - 1 && !ViewModel.IsLoading)
			{
				TranslationType = 1;
				await ViewModel.LoadDataAsync(-1, -1, ViewModel.ChapterNo + 1, 0, PreCachePolicy.CacheNext);
			}
		}

		private void SyncIndexSelection()
		{
			if (ViewModel.VolumeNo < 0 || ViewModel.ChapterNo < 0 || ViewModel.Index.Count <= ViewModel.VolumeNo || ViewModel.Index[ViewModel.VolumeNo].Count <= ViewModel.ChapterNo) return;
			var target = ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo];
			if (target != null)
				target.NotifyPropertyChanged("IsDownloaded");
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
			var scrollViewer = VolumeListView.GetScrollViewer();
			if (scrollViewer != null)
				scrollViewer.ChangeView(null, scrollViewer.VerticalOffset + 100, null, true);
		}

		//void VolumeListView_SizeChanged(object sender, SizeChangedEventArgs e)
		//{
		//	if (VolumeListView.Visibility == Windows.UI.Xaml.Visibility.Visible && e.NewSize.Height > 10 && ContentListView.Items.Count > 0)
		//	{
		//		var target = VolumeListView.GetValue(ListViewBaseScrollToItemRequestProperty);
		//		VolumeListView.SelectedItem = target;
		//		VolumeListView.UpdateLayout();
		//		VolumeListView.ScrollIntoView(target, ScrollIntoViewAlignment.Leading);
		//		VolumeListView.ClearValue(ListViewBaseScrollToItemRequestProperty);
		//		VolumeListView.SizeChanged -= VolumeListView_SizeChanged;
		//	}
		//}

		private async void ContentListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (IsIndexPanelOpen)
			{
				IsIndexPanelOpen = false;
				return;
			}

			var line = (LineViewModel)e.ClickedItem;


			var container = ContentListView.ContainerFromIndex(line.No - 1) as SelectorItem;
			var iv = container.ContentTemplateRoot as Grid;
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
				if (App.Current.IsSignedIn)
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

			CommentsFlyout.ShowAt((FrameworkElement)container);

			if (line.HasComments && !line.IsLoading)
			{
				await line.LoadCommentsAsync();
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
					if (!App.ShouldAutoLoadImage)
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
					textContent.Text = line.Content;
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
					if (App.ShouldAutoLoadImage)
						args.RegisterUpdateCallback(2, ContentListView_ContainerContentChanging);
				}
			}
			else if (args.Phase == 2)
			{
				var line = (LineViewModel)args.Item;
				LoadItemIllustation(iv, line);
			} else if (args.Phase == 3)
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

		private async void ChapterListView_ItemClick(object sender, ItemClickEventArgs e)
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

		private void IndexPanel_Closed(object sender, object e)
		{
			if (IsIndexPanelOpen)
				IsIndexPanelOpen = false;
		}

		private async void VolumeIndexItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			if (ViewModel.IsLoading)
				return;
			var vvm = (sender as FrameworkElement).DataContext as VolumeViewModel;

			if (!vvm.Chapters.Contains(VolumeListView.SelectedItem))
			{
				var cpvm = vvm.Chapters[0];
				VolumeListView.SelectedItem = cpvm;
				if (UsingLogicalIndexPage)
					IsIndexPanelOpen = false;
				if (cpvm.VolumeNo < ViewModel.VolumeNo || (cpvm.VolumeNo == ViewModel.VolumeNo && cpvm.No < ViewModel.ChapterNo))
					TranslationType = -1;
				else
					TranslationType = 1;
				await ViewModel.LoadDataAsync(-1, cpvm.VolumeNo, cpvm.No, 0);
			}
			else if (UsingLogicalIndexPage)
			{
				IsIndexPanelOpen = false;
			}
		}

		private void JumpToInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter || e.Key == VirtualKey.Accept || e.Key == VirtualKey.Search)
			{

			}
		}

		private async void JumpToButton_Click(object sender, RoutedEventArgs e)
		{
			int lineNo;
			var keyword = JumpToInputBox.Text.Trim();
			var succ = int.TryParse(keyword, out lineNo);
			if (succ)
			{
				if (lineNo >= 1 && lineNo <= ViewModel.Contents.Count)
					ContentListView.ScrollIntoView(ContentListView.Items[lineNo - 1], ScrollIntoViewAlignment.Leading);
				else
				{
					MessageDialog dialog = new MessageDialog("Line No out of range", "Jump Failed");
					await dialog.ShowAsync();
				}
			}
			else
			{
				lineNo = GetCurrentLineNo();
				var list = ViewModel.ChapterData.Lines as List<Service.Line>;
				lineNo = list.FindIndex(lineNo, l => (l.ContentType == LineContentType.TextContent && l.Content.Contains(keyword)));
				if (lineNo >= 0)
					ContentListView.ScrollIntoView(ContentListView.Items[lineNo], ScrollIntoViewAlignment.Leading);
				else
				{
					MessageDialog dialog = new MessageDialog("Cannot find specified keyword", "Jump Failed");
					await dialog.ShowAsync();
				}
			}
		}

		private void FontSizeButtonClick(object sender, RoutedEventArgs e)
		{
			MenuFlyout mf = (MenuFlyout)this.Resources["FontSizeFlyout"];
			mf.Placement = FlyoutPlacementMode.Bottom;
			mf.ShowAt(this.BottomAppBar);
		}

		private void FontFamilyButtonClick(object sender, RoutedEventArgs e)
		{
			MenuFlyout mf = (MenuFlyout)this.Resources["FontStyleFlyout"];
			mf.Placement = FlyoutPlacementMode.Bottom;
			mf.ShowAt(this.BottomAppBar);
		}

		private void ReadingThemeButtonClick(object sender, RoutedEventArgs e)
		{
			MenuFlyout mf = (MenuFlyout)this.Resources["ReadingThemeFlyout"];
			mf.Placement = FlyoutPlacementMode.Bottom;
			mf.ShowAt(this.BottomAppBar);
		}
		private void JumpToAppBarButton_Click(object sender, RoutedEventArgs e)
		{
			Flyout mf = (Flyout)this.Resources["JumpToFlyout"];
			mf.Placement = FlyoutPlacementMode.Bottom;
			mf.ShowAt(this.BottomAppBar);
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

		private async void IllustrationRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			var lvm = ((FrameworkElement)sender).DataContext as LineViewModel;
			if (lvm.IsImageCached) // LocalUri
			{
				var container = ContentListView.ContainerFromIndex(lvm.No - 1) as SelectorItem;
				if (container != null)
				{
					var iv = container.ContentTemplateRoot as Grid; 
					HideImageRefreshButton();
					await RefreshCruptedImageItem(iv);
				}
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

		private void FullScreenButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.IsFullScreen = !ViewModel.IsFullScreen;
		}

		private async void RefreshContentButtonClick(object sender, RoutedEventArgs e)
		{
			await ViewModel.RefreshSeriesDataAsync();
		}


	}
}