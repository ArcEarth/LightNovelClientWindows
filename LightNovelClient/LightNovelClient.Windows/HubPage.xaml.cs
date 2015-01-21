using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using Q42.WinRT.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinRTXamlToolkit.Controls.Extensions;

//// The Universal Hub Application project template is documented at http://go.microsoft.com/fwlink/?LinkID=391955

namespace LightNovel
{
	/// <summary>
	/// A page that displays a grouped collection of items.
	/// </summary>
	public sealed partial class HubPage : Page
	{
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
			//var sampleDataGroup = await SampleDataSource.GetGroupAsync("Group-4");

			//Task<Session> LoginTask = null;
			Task LoadingIndexTask = null;
			Task LoadingRecommandTask = null;
			//Task LoadingFavouriteTask = null;
			Task LoadingRecentTask = null;
			Task LoginTask = null;

			ViewModel.IsLoading = true;

			if (!ViewModel.RecommandSection.IsLoaded && !ViewModel.RecommandSection.IsLoading)
			{
				LoadingRecommandTask = ViewModel.RecommandSection.LoadAsync();
			}
			if (ViewModel.SeriesIndex == null)
			{
				LoadingIndexTask = ViewModel.LoadSeriesIndexDataAsync().ContinueWith(async task =>
				{
					await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						if (SeriesIndexViewSource.View == null)
						{
							SeriesIndexViewSource.IsSourceGrouped = true;
							SeriesIndexViewSource.Source = ViewModel.SeriesIndex;
						}
						if (SeriesIndexViewSource.View != null)
							ViewModel.SeriesIndexGroupView = SeriesIndexViewSource.View.CollectionGroups;
					});
				});
			}

			LoadingRecentTask = ViewModel.RecentSection.LoadLocalAsync();

			if (!App.Current.IsSignedIn)
			{
				LoginTask = ViewModel.TryLogInWithStoredCredentialAsync().ContinueWith(async task =>
				{
					if (task.Result)
					{
						await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
						{
							await ViewModel.FavoriteSection.LoadAsync();
						});
					}
				});
			}
			else
			{
				ViewModel.IsSignedIn = true;
				ViewModel.UserName = App.Current.User.UserName;
				LoginTask = ViewModel.FavoriteSection.LoadAsync();
			}

#if WINDOWS_APP
			if (HubScrollViewer != null)
				HubScrollViewer.ViewChanged += HubScrollViewer_ViewChanged;
#else //WINDOWS_PHONE_APP
			var statusBar = StatusBar.GetForCurrentView();
			statusBar.ProgressIndicator.Text = "Synchronizing...";
			statusBar.ProgressIndicator.ProgressValue = null;
			statusBar.ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBackgroundBrush"]).Color;
			await statusBar.HideAsync();
			await statusBar.ProgressIndicator.ShowAsync();
#endif
			if (LoadingRecentTask != null)
			{
				await LoadingRecentTask;
				if (ViewModel.RecentSection.Count > 0)
				{
					ViewModel.LastReadSection = ViewModel.RecentSection.FirstOrDefault();
					ViewModel.RecentSection.RemoveAt(0);
				}
			}
			if (LoadingRecommandTask != null)
				await LoadingRecommandTask;
			if (LoadingIndexTask != null)
				await LoadingIndexTask;

			if (ViewModel.LastReadSection == null)
			{
				var bvm = ViewModel.RecommandSection[0][0];
				ViewModel.LastReadSection = new HistoryItemViewModel
				{
					Position = new NovelPositionIdentifier
					{
						SeriesId = bvm.Id,
						VolumeNo = 0,
						ChapterNo = 0,
					},
					SeriesTitle = bvm.Title,
					Description = bvm.Description,
					CoverImageUri = bvm.CoverImageUri,
				};
				try
				{
					var series = await CachedClient.GetSeriesAsync(bvm.Id);
					var chapter = await CachedClient.GetChapterAsync(series.Volumes[0].Chapters[0].Id);
					var imageLine = chapter.Lines.FirstOrDefault(line => line.ContentType == LineContentType.ImageContent);
					ViewModel.LastReadSection.CoverImageUri = imageLine.Content;
					ViewModel.LastReadSection.Description = series.Description;
				}
				catch (Exception)
				{
					Debug.WriteLine("Failed to download the Cover Image.");
				}
			}

			if (LoginTask != null)
				await LoginTask;

			ViewModel.IsLoading = false;

#if  WINDOWS_APP
			foreach (var group in ViewModel.RecommandSection)
			{
				foreach (var item in group)
				{
					try
					{
						await item.LoadDescriptionAsync();
					}
					catch (Exception exception)
					{
						Debug.WriteLine("Exception in loading volume description : ({0},{1}), exception : {3}", item.Title, item.Id, exception.Message);
					}

				}
			}
#else
			await statusBar.ProgressIndicator.HideAsync();
#endif
		}

	}
}
