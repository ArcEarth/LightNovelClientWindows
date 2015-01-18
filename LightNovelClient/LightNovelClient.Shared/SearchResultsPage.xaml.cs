﻿using LightNovel.Common;
using LightNovel.ViewModels;
using LightNovel.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Threading.Tasks;

// TODO: Connect the Search Results Page to your in-app search.
// The Search Results Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234240

namespace LightNovel
{
	/// <summary>
	/// This page displays search results when a global search is directed to this application.
	/// </summary>
	public sealed partial class SearchResultsPage : Page
	{
		private NavigationHelper navigationHelper;
		private ObservableDictionary defaultViewModel = new ObservableDictionary();

		string _QueryText;
		public string QueryText { 
			get
			{ return _QueryText; }
			set
			{
				_QueryText = value;
				this.DefaultViewModel["QueryText"] = _QueryText;
			}
		}
		public List<BookItem> Results { get; set; }
		public Task QueryTask { get; set; }
		//private bool IsPageActive = true;
		/// <summary>
		/// This can be changed to a strongly typed view model.
		/// </summary>
		public ObservableDictionary DefaultViewModel
		{
			get { return this.defaultViewModel; }
		}
		/// <summary>
		/// NavigationHelper is used on each page to aid in navigation and 
		/// process lifetime management
		/// </summary>
		public NavigationHelper NavigationHelper
		{
			get { return this.navigationHelper; }
		}

		public SearchResultsPage()
		{
			this.InitializeComponent();
			this.navigationHelper = new NavigationHelper(this);
			this.navigationHelper.LoadState += navigationHelper_LoadState;
			this.navigationHelper.SaveState += navigationHelper_SaveState;
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
			if (e.PageState != null)
			{
				QueryText = (string)e.PageState["QueryText"];
				var results = (string)e.PageState["Results"];
				QueryTask = null;
				Results = JsonConvert.DeserializeObject<List<BookItem>>(results);
			}
			else
			{
				QueryText = e.NavigationParameter as String;
				QueryTask = LightNovel.Service.LightKindomHtmlClient.SearchBookAsync(QueryText).ContinueWith(async result =>
				{
					Results = result.Result;
					var bvms = from book in Results group new BookCoverViewModel(book) by book.Title into g select g;
					this.DefaultViewModel["Results"] = bvms;
					await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,()=>{
						resultsZoomedOutView.ItemsSource = resultsViewSource.View.CollectionGroups;
						if (Results == null || Results.Count == 0)
							VisualStateManager.GoToState(this, "NoResultsFound", true);
						else
						{
							VisualStateManager.GoToState(this, "ResultsFound", true);
						}
					});

				});
			}

			// TODO: Application-specific searching logic.  The search process is responsible for
			//       creating a list of user-selectable result categories:
			//
			//       filterList.Add(new Filter("<filter name>", <result count>));
			//
			//       Only the first filter, typically "All", should pass true as a third argument in
			//       order to start in an active state.  Results for the active filter are provided
			//       in Filter_SelectionChanged below.

			var filterList = new List<Filter>();
			filterList.Add(new Filter("All", 0, true));
			this.DefaultViewModel["Filters"] = filterList;

			// Communicate results through the view model
			this.DefaultViewModel["ShowFilters"] = filterList.Count > 1;

			if (QueryTask != null)
				await QueryTask;
		}
		private void navigationHelper_SaveState(object sender, SaveStateEventArgs e)
		{
			e.PageState.Add("QueryText", QueryText);
			var results = JsonConvert.SerializeObject(Results);
			e.PageState.Add("Results", results);
		}

		void ResultsGridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			// Navigate to the appropriate destination page, configuring the new page
			// by passing required information as a navigation parameter
			var book = (BookCoverViewModel)e.ClickedItem;
			if (book.ItemType == LightNovel.Service.BookItemType.Volume)
			{
				this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { VolumeId = book.Id }.ToString());
			}
			else if (book.ItemType == LightNovel.Service.BookItemType.Series)
			{
				this.Frame.Navigate(typeof(ReadingPage), new NovelPositionIdentifier { SeriesId = book.Id }.ToString());
			}
		}


		/// <summary>
		/// Invoked when a filter is selected using a RadioButton when not snapped.
		/// </summary>
		/// <param name="sender">The selected RadioButton instance.</param>
		/// <param name="e">Event data describing how the RadioButton was selected.</param>
		void Filter_Checked(object sender, RoutedEventArgs e)
		{
			var filter = (sender as FrameworkElement).DataContext;

			// Mirror the change into the CollectionViewSource.
			// This is most likely not needed.
			if (filtersViewSource.View != null)
			{
				filtersViewSource.View.MoveCurrentTo(filter);
			}

			// Determine what filter was selected
			var selectedFilter = filter as Filter;
			if (selectedFilter != null)
			{
				// Mirror the results into the corresponding Filter object to allow the
				// RadioButton representation used when not snapped to reflect the change
				selectedFilter.Active = true;

				// TODO: Respond to the change in active filter by setting this.DefaultViewModel["Results"]
				//       to a collection of items with bindable Image, Title, Subtitle, and Description properties

				// Ensure results are found
				object results;
				ICollection resultsCollection;
				if (this.DefaultViewModel.TryGetValue("Results", out results) &&
					(resultsCollection = results as ICollection) != null &&
					resultsCollection.Count != 0)
				{
					VisualStateManager.GoToState(this, "ResultsFound", true);
					return;
				}
			}

			// Display informational text when there are no search results.
			VisualStateManager.GoToState(this, "NoResultsFound", true);
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

		/// <summary>
		/// View model describing one of the filters available for viewing search results.
		/// </summary>
		private sealed class Filter : INotifyPropertyChanged
		{
			private String _name;
			private int _count;
			private bool _active;

			public Filter(String name, int count, bool active = false)
			{
				this.Name = name;
				this.Count = count;
				this.Active = active;
			}

			public override String ToString()
			{
				return Description;
			}

			public String Name
			{
				get { return _name; }
				set { if (this.SetProperty(ref _name, value)) this.OnPropertyChanged("Description"); }
			}

			public int Count
			{
				get { return _count; }
				set { if (this.SetProperty(ref _count, value)) this.OnPropertyChanged("Description"); }
			}

			public bool Active
			{
				get { return _active; }
				set { this.SetProperty(ref _active, value); }
			}

			public String Description
			{
				get { return String.Format("{0} ({1})", _name, _count); }
			}

			/// <summary>
			/// Multicast event for property change notifications.
			/// </summary>
			public event PropertyChangedEventHandler PropertyChanged;

			/// <summary>
			/// Checks if a property already matches a desired value.  Sets the property and
			/// notifies listeners only when necessary.
			/// </summary>
			/// <typeparam name="T">Type of the property.</typeparam>
			/// <param name="storage">Reference to a property with both getter and setter.</param>
			/// <param name="value">Desired value for the property.</param>
			/// <param name="propertyName">Name of the property used to notify listeners.  This
			/// value is optional and can be provided automatically when invoked from compilers that
			/// support CallerMemberName.</param>
			/// <returns>True if the value was changed, false if the existing value matched the
			/// desired value.</returns>
			private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
			{
				if (object.Equals(storage, value)) return false;

				storage = value;
				this.OnPropertyChanged(propertyName);
				return true;
			}

			/// <summary>
			/// Notifies listeners that a property value has changed.
			/// </summary>
			/// <param name="propertyName">Name of the property used to notify listeners.  This
			/// value is optional and can be provided automatically when invoked from compilers
			/// that support <see cref="CallerMemberNameAttribute"/>.</param>
			private void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				var eventHandler = this.PropertyChanged;
				if (eventHandler != null)
				{
					eventHandler(this, new PropertyChangedEventArgs(propertyName));
				}
			}

		}

		private void queryText_TextChanged(object sender, TextChangedEventArgs e)
		{

		}
	}
}