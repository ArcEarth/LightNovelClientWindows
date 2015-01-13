using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;
using LightNovel.ViewModels;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.ComponentModel;

namespace LightNovel
{

	public abstract class DataTemplateSelector : ContentControl
	{
		public virtual DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			return null;
		}

		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);

			ContentTemplate = SelectTemplate(newContent, this);
		}
	}

	public class NovelLineDataTemplateSelector : DataTemplateSelector
	{
		public DataTemplate IllustrationDataTemplate { get; set; }
		public DataTemplate TextDataTemplate { get; set; }
		public DataTemplate ImageLoadingDataTemplate { get; set; }
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			//FrameworkElement element = container as FrameworkElement;
			if (item != null && item is LineViewModel)
			{
				LineViewModel line = item as LineViewModel;

				if (line is IllustrationViewModel)
					return IllustrationDataTemplate;
				else
					return TextDataTemplate;
			}

			return base.SelectTemplate(item, container);
		}

	}

	public class LongListSelectorOberserver
	{
		private LongListSelector lls;
		private ViewportControl viewportControl;
		private readonly Dictionary<object, ContentPresenter> Items = new Dictionary<object, ContentPresenter>();

		public LongListSelectorOberserver(LongListSelector lls)
		{
			this.lls = lls;
			viewportControl = FindViewport(lls);
			lls.ItemRealized += LLS_ItemRealized;
			lls.ItemUnrealized += LLS_ItemUnrealized;
			//lls.ManipulationStateChanged += LLS_ManipulationStateChanged;
			//lls.ManipulationDelta +=lls_ManipulationDelta;
			//lls.MouseMove += listbox_MouseMove;
		}

		//private void lls_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
		//{
		//}

		public enum StretchingDirection
		{
			Up,
			Dwon,
			Left,
			Right
		}

		public class StretchingEventArgs : EventArgs
		{
			public double Delta { get; set; }
			public StretchingDirection Direction { get; set; }
		}

		public IEnumerable<object> GetItemsRealized()
		{
			return Items.Keys;
		}
		public IEnumerable<object> GetItemsInView()
		{
			if (viewportControl == null)
				viewportControl = FindViewport(lls);
			if (viewportControl == null)
				return null;
			var viewport = viewportControl.Viewport;
			var topOffset = viewport.Top;
			var bottomOffset = viewport.Bottom;
			return Items.Where(x => Canvas.GetTop(x.Value) + x.Value.ActualHeight > topOffset && Canvas.GetTop(x.Value) < bottomOffset)
				.OrderBy(x => Canvas.GetTop(x.Value)).Select(x => x.Key);
		}
		public object GetFirstVisibleItem()
		{
			return GetItemsInView().FirstOrDefault();
		}

		public object GetLastVisibleItem()
		{
			return GetItemsInView().LastOrDefault();
		}

		public bool IsStrectchingTop()
		{
			if (lls.ManipulationState != ManipulationState.Animating)
				return false;
			var obj = GetFirstVisibleItem();
			if (lls.ItemsSource[0] != obj)
				return false;
			var offset = viewportControl.Viewport.Top;
			var visual = Items[obj];
			var obj_offset = Canvas.GetTop(visual);
			if (obj_offset == offset)
				return true;
			return false;
		}
		internal bool IsStrectchingBottom()
		{
			if (lls.ManipulationState != ManipulationState.Animating)
				return false;
			var obj = GetItemsInView().LastOrDefault();
			if (lls.ItemsSource[lls.ItemsSource.Count - 1] != obj)
				return false;
			var offset = viewportControl.Viewport.Bottom;
			var visual = Items[obj];
			var obj_offset = Canvas.GetTop(visual) + visual.ActualHeight;
			if (obj_offset == offset)
				return true;
			return false;
		}

		public void Dispose()
		{
			lls.ItemRealized -= LLS_ItemRealized;
			lls.ItemUnrealized -= LLS_ItemUnrealized;
		}


		private void LLS_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			if (e.ItemKind == LongListSelectorItemKind.Item)
			{
				object o = e.Container.DataContext;
				Items[o] = e.Container;
			}
		}

		private void LLS_ItemUnrealized(object sender, ItemRealizationEventArgs e)
		{
			if (e.ItemKind == LongListSelectorItemKind.Item)
			{
				//Debug.WriteLine();
				object o = e.Container.DataContext;
				Items.Remove(o);
			}
		}

		private static ViewportControl FindViewport(DependencyObject parent)
		{
			var childCount = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < childCount; i++)
			{
				var elt = VisualTreeHelper.GetChild(parent, i);
				if (elt is ViewportControl) return (ViewportControl)elt;
				var result = FindViewport(elt);
				if (result != null) return result;
			}
			return null;
		}
	}
	public partial class ChapterViewPage : PhoneApplicationPage
	{
		private readonly LongListSelectorOberserver _llsOberserver;
		public ChapterViewModel ViewModel
		{
			get { return DataContext as ChapterViewModel; }
		}
		public ChapterViewPage()
		{
			InitializeComponent();
			DataContext = App.ChapterPageViewModel;
			//ContentListView.ItemUnrealized += RecordingProgress_ItemUnrealized;
			//ContentListView.ItemRealized += ContentListView_ItemRealized;
			_llsOberserver = new LongListSelectorOberserver(ContentListView);
			ContentListView.ManipulationStateChanged += ContentListView_ManipulationStateChanged;
			//ContentListView.ManipulationDelta +=ContentListView_ManipulationDelta;
			ContentListView.ItemRealized += ContentListView_ItemRealized_LoadComments;

			//ContentListView.Loaded += ContentListView_Loaded;
		}

		protected override void OnBackKeyPress(CancelEventArgs e)
		{
			if (!NavigationService.BackStack.First().Source.ToString().Contains("SeriesViewPage.xaml"))
			{
				var serialUri = new Uri(String.Format("/SeriesViewPage.xaml?id={0}&volume={1}", App.CurrentSeries.Id , App.CurrentVolume.Id), UriKind.Relative);
				NavigationService.Navigate(serialUri);
			}
			else
			{
				base.OnBackKeyPress(e);
			}
		}

		private void ContentListView_Loaded(object sender, RoutedEventArgs e)
		{
			int lineNo = ViewModel.CurrentLineNo;
			if (ContentListView.ItemsSource.Count > 0 && ContentListView.ItemsSource.Count > lineNo)
			{
				Dispatcher.BeginInvoke(() => ContentListView.ScrollTo(ContentListView.ItemsSource[lineNo]));
			}
			else
				throw new System.IndexOutOfRangeException("ContentListView don't have a line at No." + lineNo);
		}

		//void ContentListView_ManipulationDelta(object sender, System.Windows.Input.ManipulationDeltaEventArgs e)
		//{
		//    Debug.WriteLine("Manipulation Delta Fired!");
		//}

		async void ContentListView_ManipulationStateChanged(object sender, EventArgs e)
		{
			var line = _llsOberserver.GetFirstVisibleItem() as LineViewModel;
			if (line != null)
			{
				ViewModel.CurrentLineNo = line.Id;
				Debug.WriteLine("Current Line : " + line.Id);
				if (_llsOberserver.IsStrectchingTop())
				{
					Debug.WriteLine("Streching Top detected!");
					if (!String.IsNullOrEmpty(ViewModel.PrevChapterId))
					{
						await ViewModel.LoadPrevChapterAsync(App.Settings.EnableComments);
						ContentListView.ItemRealized += ContentListView_ItemRealized_ScrollToCurrentLine;
					}
				}
				if (_llsOberserver.IsStrectchingBottom())
				{
					Debug.WriteLine("Streching Top detected!");
					if (!String.IsNullOrEmpty(ViewModel.NextChapterId))
					{
						await ViewModel.LoadNextChapterAsync(App.Settings.EnableComments);
					}
				}
			}
		}

		async void ContentListView_ItemRealized_LoadComments(object sender, ItemRealizationEventArgs e)
		{
			//if (e.ItemKind == LongListSelectorItemKind.ListFooter)
			//{
			//    await chapter.LoadNextChapterAsync();
			//}

			if (e.ItemKind == LongListSelectorItemKind.Item)
			{
				var line = e.Container.Content as LineViewModel;
				if (line != null && line.HasComments && line.Comments.Count == 0 && !line.IsLoading)
				{
					await ViewModel.LoadCommentsAsync(line);
					//Debug.WriteLine(chapter.ReadingProgress);
				}
			}
		}
		void ContentListView_ItemRealized_ScrollToCurrentLine(object sender, ItemRealizationEventArgs e)
		{
			ContentListView.ItemRealized -= ContentListView_ItemRealized_ScrollToCurrentLine;
			ContentListView.ScrollTo(ContentListView.ItemsSource[ViewModel.CurrentLineNo - 1]);
		}

		/// <summary>
		/// Called when a page becomes the active page in a frame.
		/// </summary>
		/// <param name="e">An object that contains the event data.</param>
		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			if (e.NavigationMode == NavigationMode.Back)
				return;

			Debug.WriteLine(e.NavigationMode);
			string id = NavigationContext.QueryString["id"];
			int lineNo = 1;
			if (NavigationContext.QueryString.ContainsKey("line"))
			{
				string line = NavigationContext.QueryString["line"];
				lineNo = int.Parse(line);
			}
			var vm = ViewModel;
			if (vm.ChapterId != id && !vm.IsLoading)
			{
				await vm.LoadDataAsync(id,App.Settings.EnableComments);
				if (vm.ChapterId == null)
				{
					vm.IsLoading = false;
					NavigationService.GoBack();
					return;
				}
			}
			ViewModel.CurrentLineNo = lineNo;
			if (ContentListView.ItemsSource.Count > 0 && ContentListView.ItemsSource.Count > lineNo)
			{
				ContentListView.ItemRealized += ContentListView_ItemRealized_ScrollToCurrentLine;
			}
			else
			{
				Debug.WriteLine("LineNo except the range of lines, Target Line # : " + lineNo);
			}
			vm.IsLoading = false;
		}

		/// <summary>
		/// Called when a page is no longer the active page in a frame.
		/// </summary>
		/// <param name="e">An object that contains the event data.</param>
		protected async override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (ViewModel.ChapterId == null )
				return;
			;
			var bookmark = ViewModel.CreateBookmarkFromCurrentPage();
			var linesInView = _llsOberserver.GetItemsInView();
			foreach (LineViewModel lineView in linesInView)
			{
				if (lineView.IsImage)
					bookmark.DescriptionImageUri = (lineView as IllustrationViewModel).NonCachedImageUri.AbsoluteUri;
				else
					bookmark.ContentDescription += lineView.Content;
			}

			App.HistoryList.RemoveAll(item => item.Position.SeriesId == bookmark.Position.SeriesId);
			App.HistoryList.Add(bookmark);
			if (e.NavigationMode == NavigationMode.Back) // Reset the chapter info, only in the case of navigate back
			{
				ViewModel.ChapterId = null;
				ViewModel.Title = "Loading...";
			}
			await App.SaveHistoryListAsync();
			base.OnNavigatedFrom(e);
		}
		//private async void Left_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		//{
		//    var vm = ViewModel;
		//    if (!vm.IsLoading && vm.PrevChapterId != null)
		//    {
		//        await vm.LoadPrevChapterAsync();
		//        Dispatcher.BeginInvoke(() =>
		//        {
		//            ContentListView.ScrollTo(ContentListView.ItemsSource[ContentListView.ItemsSource.Count - 1]);
		//        });
		//    }
		//}

		//private async void Right_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		//{
		//    var chapter = ViewModel;
		//    if (!chapter.IsLoading && chapter.NextChapterId != null)
		//    {
		//        await chapter.LoadNextChapterAsync();
		//    }
		//}

		private void FontSizeIncreseButton_Click(object sender, EventArgs e)
		{

		}

		private void FontSizeDecreaseButton_Click(object sender, EventArgs e)
		{

		}

		private void BookmarkButton_Click(object sender, RoutedEventArgs e)
		{
			var bookmark = ViewModel.CreateBookmarkFromCurrentPage();
			App.BookmarkList.Add(bookmark);
			//var notification = new ShellToast
			//{
			//    Content = "Successfully bookmarked current page , swipe left to view all bookmark for current series.",
			//    Title = "Light Novel Client",
			//    NavigationUri = new Uri("")
			//};
			//notification.Show();
		}

	}
}