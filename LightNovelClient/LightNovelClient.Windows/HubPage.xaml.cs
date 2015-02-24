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
			//Task LoadingRecentTask = null;
			Task LoginTask = null;

			if (this.RequestedTheme != App.Current.Settings.BackgroundTheme)
			{
				this.RequestedTheme = App.Current.Settings.BackgroundTheme;
			} 
			ViewModel.IsLoading = true;

			if (App.Current.RecentList == null)
				await App.Current.LoadHistoryDataAsync();
			if (App.Current.RecentList.Count > 0)
			{
				ViewModel.LastReadSection = new HistoryItemViewModel(App.Current.RecentList[App.Current.RecentList.Count - 1]);
				await ViewModel.RecentSection.LoadLocalAsync(true,9);
			}
			else
			{
				ViewModel.LastReadSection = new HistoryItemViewModel
				{
					Position = new NovelPositionIdentifier
					{
						SeriesId = "337",
						VolumeNo = 0,
						VolumeId = "1138",
						ChapterNo = 0,
						ChapterId = "8683"
					},
					SeriesTitle = "机巧少女不会受伤",
					VolumeTitle = "第一卷 ",
					Description = "机巧魔术──那是由内藏魔术回路的自动人偶与人偶使所使用的魔术。在英国最高学府的华尔普吉斯皇家机巧学院里，正举行着一场选出顶尖人偶使「魔王」的战斗「夜会」。来自日本的留学生雷真和他的搭档──少女型态的人偶夜夜，为了参加「夜会」，打算挑战其他入选者，夺取对方的资格。他们锁定的目标是下届魔王呼声极高的候选人，别名「暴龙」的美少女夏琳！然而就在雷真向她挑战时，突然出现意外的伏兵……？ 交响曲式学园战斗动作剧，第一集登场！",
					CoverImageUri = "http://lknovel.lightnovel.cn/illustration/image/20120813/20120813085826_34455.jpg"
				};
			}

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

			if (!App.Current.IsSignedIn)
			{
				LoginTask = ViewModel.TryLogInWithStoredCredentialAsync().ContinueWith(async task => {
					await ViewModel.FavoriteSection.LoadAsync(false, 9);
				});
			}
			else
			{
				ViewModel.IsSignedIn = true;
				ViewModel.UserName = App.Current.User.UserName;
				LoginTask = ViewModel.FavoriteSection.LoadAsync(false, 9);
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

			if (LoadingRecommandTask != null)
				await LoadingRecommandTask;
			if (LoginTask != null)
				await LoginTask;
			if (LoadingIndexTask != null)
				await LoadingIndexTask;

			UpdateTile();

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

			if (App.Current.Settings.EnableLiveTile)
				UpdateTile();
#else
			await statusBar.ProgressIndicator.HideAsync();
#endif
		}
		void HubScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			double maxX = (double)App.Current.Resources["PosterWidth"];
			ScrollViewer viewer = sender as ScrollViewer;
			LogoImageTranslate.X = Math.Max(-viewer.HorizontalOffset, -maxX);
		}

		private void SeriesIndexButton_Click(object sender, RoutedEventArgs e)
		{

		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			Windows.UI.ApplicationSettings.SettingsPane.Show();
		}
	}
}
