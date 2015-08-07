using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;

// TODO: Connect the Search Results Page to your in-app search.
// The Search Results Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234240

namespace LightNovel
{
	/// <summary>
	/// This page displays search results when a global search is directed to this application.
	/// </summary>
	public class SeriesIndexViewModel : INotifyPropertyChanged
	{
		IObservableVector<object> _SeriesIndexGroupView;
		public IObservableVector<object> SeriesIndexGroupView
		{
			get { return _SeriesIndexGroupView; }
			set
			{
				if (_SeriesIndexGroupView == value) return;
				_SeriesIndexGroupView = value;
				NotifyPropertyChanged();
			}
		}

		private IList<IGrouping<string, Descriptor>> _seriesIndex;
		public IList<IGrouping<string, Descriptor>> SeriesIndex {
			get { 
				return _seriesIndex;
			}
			set { 
				_seriesIndex = value;
				NotifyPropertyChanged();
			}
		}
		private bool _isLoaded;
		private bool _isLoading;
		public bool IsLoading
		{
			get { return _isLoading; }
			set
			{
				_isLoading = value;
				NotifyPropertyChanged();
			}
		}
		public bool IsLoaded
		{
			get { return _isLoaded; }
			set
			{
				_isLoaded = value;
				NotifyPropertyChanged();
			}
		}

		public async Task LoadSeriesIndexDataAsync()
		{
			if (IsLoaded) return;
			IsLoading = true;
			try
			{
				var serIndex = await CachedClient.GetSeriesIndexAsync();

				var cgs = new Windows.Globalization.Collation.CharacterGroupings();
				SeriesIndex = (from series in serIndex
							   group series
							   by cgs.Lookup(series.Title) into g
							   orderby g.Key
							   select g).ToList();

				IsLoaded = true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Exception when retrieving series index : " + exception.Message);
				//throw exception;
				//MessageBox.Show(exception.Message, "Data error", MessageBoxButton.OK);
			}
			IsLoading = false;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

	}

	public sealed partial class SeriesIndexPage : Page
	{
		private NavigationHelper navigationHelper;

		public SeriesIndexViewModel _viewModel = new SeriesIndexViewModel();
		public SeriesIndexViewModel ViewModel
		{
			get { return _viewModel; }
		}
		/// <summary>
		/// NavigationHelper is used on each page to aid in navigation and 
		/// process lifetime management
		/// </summary>
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}

		public SeriesIndexPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += navigationHelper_LoadState;
			this.navigationHelper.SaveState += navigationHelper_SaveState;

            this.SizeChanged += SeriesIndexPage_SizeChanged;
		}

        private void SeriesIndexPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().Orientation == ApplicationViewOrientation.Landscape)
            {
                SwitchGridViewOrientation(resultsView.ZoomedInView as GridView, Orientation.Horizontal);
                SwitchGridViewOrientation(resultsView.ZoomedOutView as GridView, Orientation.Horizontal);
            }
            else
            {
                SwitchGridViewOrientation(resultsView.ZoomedInView as GridView, Orientation.Vertical, -1);
                SwitchGridViewOrientation(resultsView.ZoomedOutView as GridView, Orientation.Vertical, -1);
            }
        }

        public static void SwitchGridViewOrientation(GridView gridView, Orientation orientation, int maxItemsPerRow = -1)
        {
            if (gridView == null) return;
            var wrapGrid = gridView.ItemsPanelRoot as ItemsWrapGrid;

            // Desiered GridView Orientation should be oppsite with WrapGrid's major layout orientation
            if (wrapGrid == null || wrapGrid.Orientation != orientation)
                return;

            var scrollViwer = gridView.GetFirstDescendantOfType<ScrollViewer>();
            if (orientation == Orientation.Horizontal)
            {
                scrollViwer.VerticalScrollMode = ScrollMode.Disabled;
                scrollViwer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                scrollViwer.HorizontalScrollMode = ScrollMode.Enabled;
                scrollViwer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                wrapGrid.Orientation = Orientation.Vertical;
                wrapGrid.MaximumRowsOrColumns = maxItemsPerRow;
            }
            else
            {
                scrollViwer.VerticalScrollMode = ScrollMode.Enabled;
                scrollViwer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViwer.HorizontalScrollMode = ScrollMode.Disabled;
                scrollViwer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                wrapGrid.Orientation = Orientation.Horizontal;
                wrapGrid.MaximumRowsOrColumns = maxItemsPerRow;
            }
        }

        private void IndexListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			// Navigate to the appropriate destination page, configuring the new page
			// by passing required information as a navigation parameter
			var ser = (Descriptor)e.ClickedItem;
			this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { SeriesId = ser.Id, VolumeNo = -1, ChapterNo = -1 }.ToString());
		}
		void Filter_Checked(object sender, RoutedEventArgs e)
		{
		}
		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="navigationParameter">The parameter value passed to
		/// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
		/// </param>
		/// <param name="pageState">A dictionary of state preserved by this page during an earlier
		/// session.  This will be null the first time a page is visited.</param>
		private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
		{
			await ViewModel.LoadSeriesIndexDataAsync();
			//if (SeriesIndexViewSource.View == null)
			//{
			//	SeriesIndexViewSource.IsSourceGrouped = true;
			//	SeriesIndexViewSource.Source = ViewModel.SeriesIndex;
			//}
			if (SeriesIndexViewSource.View != null)
				ViewModel.SeriesIndexGroupView = SeriesIndexViewSource.View.CollectionGroups;
			ViewModel.IsLoaded = true;
		}
		private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
		}

		#region NavigationHelper registration

		/// The methods provided in this section are simply used to allow
		/// NavigationHelper to respond to the page's navigation methods.
		/// 
		/// Page specific logic should be placed in event handlers for the  
		/// <see cref="GridCS.Common.NavigationHelper.LoadState"/>
		/// and <see cref="GridCS.Common.NavigationHelper.SaveState"/>.
		/// The navigation parameter is available in the LoadState method 
		/// in addition to page state preserved during an earlier session.

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			navigationHelper.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			navigationHelper.OnNavigatedFrom(e);
		}

		#endregion

		public bool IsLoaded { get; set; }

		public bool IsLoading { get; set; }

        private void GridView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().Orientation == ApplicationViewOrientation.Landscape)
            {
                SwitchGridViewOrientation(sender as GridView, Orientation.Horizontal);
            }
            else
            {
                SwitchGridViewOrientation(sender as GridView, Orientation.Vertical);
            }
        }
    }
}
