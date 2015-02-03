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
using Windows.UI.Xaml.Shapes;
using WinRTXamlToolkit.Controls.Extensions;
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
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
			this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
			this.navigationHelper.GoBackCommand = new LightNovel.Common.RelayCommand(() => this.GoBack(), () => this.CanGoBack());
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
			RegisterForShare();
			Flyout.SetAttachedFlyout(this, ImagePreviewFlyout);
			RefreshThemeColor();
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
			int index = (int)ContentListView.GetValue(ContentListViewChangeViewRequestProperty);
			if (index >= 0 && e.NewSize.Height > 10 && ContentListView.Items.Count > 0 && index < ContentListView.Items.Count)
			{
				ContentListView.UpdateLayout();
				ContentListView.ScrollIntoView(ViewModel.Contents[index], ScrollIntoViewAlignment.Leading);
				index = -1;
				ContentListView.SizeChanged -= ContentListView_SizeChanged;
			}
		}

		public static readonly DependencyProperty ContentListViewChangeViewRequestProperty =
			DependencyProperty.Register("ContentListViewChangeViewRequestProperty", typeof(int),
			typeof(ListView), new PropertyMetadata(-1, null));



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
					ContentListView.SetValue(ContentListViewChangeViewRequestProperty, line);
					ContentListView.SizeChanged += ContentListView_SizeChanged;
				} else if (line < ContentListView.Items.Count)
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
			IndexPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
			VisualStateManager.GoToState(this, stateName, useTransitions);
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
				await ViewModel.LoadDataAsync(-1, -1, ViewModel.ChapterNo - 1, -1, PreCachePolicy.CachePrev );
				//ChangeView(-1, ViewModel.ChapterData.Lines.Count - 2);
			}
		}

		private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.ChapterNo < ViewModel.Index[ViewModel.VolumeNo].Chapters.Count - 1 && !ViewModel.IsLoading)
			{
				await ViewModel.LoadDataAsync(-1, -1, ViewModel.ChapterNo + 1, 0, PreCachePolicy.CacheNext);
			}
		}

		private void SyncIndexSelection()
		{
			if (ViewModel.VolumeNo < 0 || ViewModel.ChapterNo < 0) return;
			var target = ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo];
			VolumeListView.SelectedItem = target;
			VolumeListView.UpdateLayout();
			VolumeListView.ScrollIntoView(target, ScrollIntoViewAlignment.Default);
		}

		private async void ContentListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (IsIndexPanelOpen)
			{
				IsIndexPanelOpen = false;
				return;
			}

			var line = (LineViewModel)e.ClickedItem;

			if (!line.IsImage && !ViewModel.EnableComments)
				return;

			var container = ContentListView.ContainerFromItem(line);
			((FrameworkElement)CommentsFlyout.Content).DataContext = line;
			CommentsFlyout.ShowAt((FrameworkElement)container);
			//CommentInputBox.Focus(Windows.UI.Xaml.FocusState.Unfocused);
			if (line.HasComments && !line.IsLoading)
			{
				await line.LoadCommentsAsync();
			}
		}

		private async void ContentListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
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
				var line = (LineViewModel)args.Item;
				iv.Height = double.NaN;
				imageContent.Opacity = 0;
				imageContent.Height = double.NaN;
				if (!line.IsImage)
					imageContent.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
				commentIndicator.Opacity = 0;
				progressIndicator.Opacity = 0;
				textContent.Opacity = 1;
				if (line.IsImage)
					textContent.Text = "Image Loading...";
				else
					textContent.Text = line.Content;
				args.RegisterUpdateCallback(ContentListView_ContainerContentChanging); 
			//} else if (args.Phase == 1)
			//{
			//	//var textContent = iv.FindName("TextContent") as TextBlock;
			//	//textContent.Opacity = 1;
			//	//args.RegisterUpdateCallback(ContentListView_ContainerContentChanging); 
			} else if (args.Phase == 1)
			{
				var line = (LineViewModel)args.Item;
				var commentIndicator = iv.FindName("CommentIndicator") as Rectangle;
				if (line.HasComments)
					commentIndicator.Opacity = 1;
				if (line.IsImage)
					args.RegisterUpdateCallback(ContentListView_ContainerContentChanging);
			} else if (args.Phase == 2)
			{
				var line = (LineViewModel)args.Item;

				var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
				progressIndicator.Opacity = 1;
				args.RegisterUpdateCallback(ContentListView_ContainerContentChanging);
			} else if (args.Phase == 3)
			{
				var line = (LineViewModel)args.Item; 
				var bitMap = new BitmapImage(line.ImageUri);
				var imageContent = iv.FindName("ImageContent") as Image;
				var progressIndicator = iv.FindName("ProgressBar") as ProgressBar;
				bitMap.SetValue(BitmapLoadingIndicatorProperty, progressIndicator);
				bitMap.DownloadProgress += Image_DownloadProgress;
				imageContent.Source = bitMap;
				imageContent.Height = double.NaN;
				imageContent.Opacity = 1;
				//imageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
			args.Handled = true;
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
				var textContent = iv.FindName("TextContent") as TextBlock;
				var imageContent = iv.FindName("ImageContent") as Image;
				textContent.Opacity = 0;
				progressBar.Opacity = 0;
				imageContent.Visibility = Windows.UI.Xaml.Visibility.Visible;
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

		private void IndexPanel_Closed(object sender, object e)
		{
			if (IsIndexPanelOpen)
				IsIndexPanelOpen = false;
		}

		private void VolumeIndexItem_Tapped(object sender, TappedRoutedEventArgs e)
		{

		}

	}
}