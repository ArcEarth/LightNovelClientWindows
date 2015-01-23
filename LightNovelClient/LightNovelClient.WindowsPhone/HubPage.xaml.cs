﻿using LightNovel.Common;
using LightNovel.Service;
using LightNovel.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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
			ViewModel.IsLoading = true;
			var statusBar = StatusBar.GetForCurrentView();
			statusBar.ProgressIndicator.Text = "Synchronizing...";
			statusBar.ProgressIndicator.ProgressValue = null;
			statusBar.ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBackgroundBrush"]).Color;
			await statusBar.ProgressIndicator.ShowAsync();

			if (App.Current.RecentList == null)
				await App.Current.LoadHistoryDataAsync();
			if (App.Current.RecentList.Count > 0)
			{
				ViewModel.LastReadSection = new HistoryItemViewModel(App.Current.RecentList[App.Current.RecentList.Count - 1]);
				await ViewModel.RecentSection.LoadLocalAsync(true);
			} else
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

			if (!App.Current.IsSignedIn)
			{
				await ViewModel.TryLogInWithStoredCredentialAsync();
			}
			else
			{
				ViewModel.IsSignedIn = true;
				ViewModel.UserName = App.Current.User.UserName;
			}
			if (App.Current.IsSignedIn)
				await ViewModel.FavoriteSection.LoadAsync();

			ViewModel.IsLoading = false;

			await statusBar.HideAsync();
			await statusBar.ProgressIndicator.HideAsync();

		}

		private async void RootHub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
		{
			if (e.AddedSections.Contains(AllSection) && !ViewModel.IsIndexDataLoaded && !IsLoadingIndex)
			{
				IsLoadingIndex = true;
				await ViewModel.LoadSeriesIndexDataAsync();
				if (SeriesIndexViewSource.View == null)
				{
					SeriesIndexViewSource.IsSourceGrouped = true;
					SeriesIndexViewSource.Source = ViewModel.SeriesIndex;
				}
				if (SeriesIndexViewSource.View != null)
					ViewModel.SeriesIndexGroupView = SeriesIndexViewSource.View.CollectionGroups;
				IsLoadingIndex = false;
			} else if (e.AddedSections.Contains(RecommandSection) && !ViewModel.RecommandSection.IsLoading && !ViewModel.RecommandSection.IsLoaded)
			{
				await ViewModel.RecommandSection.LoadAsync();
				UpdateTile();
			}
		}

		public bool IsLoadingIndex { get; set; }
	}
}
