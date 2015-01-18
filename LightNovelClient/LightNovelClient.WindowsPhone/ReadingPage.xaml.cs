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
			//Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
			//ScrollSwitch = false;
			//DisableAnimationScrollingFlag = true;
			//ContentRegion.SizeChanged += ContentRegion_SizeChanged;
			//ContentColumns.ColumnsChanged += ContentColumns_LayoutUpdated;
			//ContentScrollViewer.LayoutUpdated += ScrollToPage_ContentColumns_LayoutUpdated;
			//ViewModel.CommentsListLoaded += ViewModel_CommentsListLoaded;
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
				if (ContentListView.Items.Count > 0 )
				{
					ContentListView.UpdateLayout();
					ContentListView.ScrollIntoView(ViewModel.Contents[line]);
				}
				else if (line < ContentListView.Items.Count)
				{
					ContentListView.SetValue(ContentListViewChangeViewRequestProperty, line);
					ContentListView.SizeChanged += ContentListView_SizeChanged;
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
				await ViewModel.LoadDataAsync(null, null, ViewModel.ChapterNo - 1, -1);
				//ChangeView(-1, ViewModel.ChapterData.Lines.Count - 2);
			}
		}

		private async void NextChapterButton_Click(object sender, RoutedEventArgs e)
		{
			if (ViewModel.ChapterNo < ViewModel.Index[ViewModel.VolumeNo].Chapters.Count - 1 && !ViewModel.IsLoading)
			{
				await ViewModel.LoadDataAsync(null, null, ViewModel.ChapterNo + 1, 0);
			}
		}

		private void SyncIndexSelection()
		{
			ListViewExtensions.SetItemToBringIntoView(VolumeListView,ViewModel.Index[ViewModel.VolumeNo][ViewModel.ChapterNo]);
		}

		private async void ContentListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var line = (LineViewModel)e.ClickedItem;

			if (!line.IsImage && !ViewModel.EnableComments)
				return;

			var container = ContentListView.ContainerFromItem(line);
			((FrameworkElement)CommentsFlyout.Content).DataContext = line;
			CommentsFlyout.ShowAt((FrameworkElement)container);
			if (line.HasComments)
			{
				await line.LoadCommentsAsync();
			}
		}
	}
}