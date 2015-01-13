using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using LightNovel.Service;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using LightNovel.ViewModels;
using GestureEventArgs = System.Windows.Input.GestureEventArgs;
using System.ComponentModel;

namespace LightNovel
{
	public partial class SeriesViewPage : PhoneApplicationPage
	{
		public SeriesViewModel ViewModel
		{
			get { return DataContext as SeriesViewModel; }
		}
		public SeriesViewPage()
		{
			InitializeComponent();
			DataContext = App.SeriesPageViewModel;
			VolumeViewList.ItemRealized += VolumeViewList_ItemRealized_ScrollLoading;
		}

		private int CurrentVolumeNo;
		void VolumeViewList_ItemRealized_ScrollToCurrentLine(object sender, ItemRealizationEventArgs e)
		{
			VolumeViewList.ItemRealized -= VolumeViewList_ItemRealized_ScrollToCurrentLine;
			VolumeViewList.ScrollTo(VolumeViewList.ItemsSource[CurrentVolumeNo]);
		}
		protected override void OnBackKeyPress(CancelEventArgs e)
		{
			while (!NavigationService.BackStack.First().Source.ToString().Contains("MainPage.xaml"))
			{
				NavigationService.RemoveBackEntry();
			}
			base.OnBackKeyPress(e);
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			var series = ViewModel;
			if (e.NavigationMode == NavigationMode.Back) return;
			//Uri e.Uri
			string serId = NavigationContext.QueryString["id"];
			string volId = null;
			if (NavigationContext.QueryString.ContainsKey("volume"))
			{
				volId = NavigationContext.QueryString["volume"];
			}
			string cptId = null;
			if (NavigationContext.QueryString.ContainsKey("chapter"))
			{
				cptId = NavigationContext.QueryString["chapter"];
			}
			int lineNo = 1;
			if (NavigationContext.QueryString.ContainsKey("line"))
			{
				string line = NavigationContext.QueryString["line"];
				lineNo = int.Parse(line);
			}
			// Volume specifid view
			if (String.IsNullOrWhiteSpace(serId) && !String.IsNullOrWhiteSpace(volId))
			{
				ViewModel.IsLoading = true;
				Volume volData = null;
				try
				{
					volData = await CachedClient.GetVolumeAsync(volId);
				}
				catch (Exception exception)
				{
					ViewModel.IsLoading = false;
					MessageBox.Show(exception.Message, "Network issue", MessageBoxButton.OK);
					NavigationService.GoBack();
					return;
				}
				serId = volData.ParentSeriesId;
				ViewModel.Title = volData.Title;
				ViewModel.VolumeList.Clear();
				ViewModel.VolumeList.Add(new VolumeViewModel{DataContext = volData});
				ViewModel.Id = serId;
				App.CurrentVolume = volData;
				try
				{
					var serData = await CachedClient.GetSeriesAsync(serId);
					for (int i = 0; i < serData.Volumes.Count; i++)
					{
						if (serData.Volumes[i].Id == volId) 
						{
							CurrentVolumeNo = i;
							break;
						}
					}
					ViewModel.DataContext = serData;
					if (VolumeViewList.ItemsSource.Count > 0)
					{
						VolumeViewList.ItemRealized += VolumeViewList_ItemRealized_ScrollToCurrentLine;
					}
					//App.CurrentSeries = serData;
				}
				catch (Exception exception)
				{
					MessageBox.Show("Exception happend when retriving series data" + exception.Message, "Network issue", MessageBoxButton.OK);
				}
			}
			
			if (series.Id != serId && !series.IsLoading)
			{
				await series.LoadDataAsync(serId);
				//ViewModel.IsLoading = true;
				if (VolumeViewList.ItemsSource.Count > 0)
				{
					CurrentVolumeNo = 0;
					VolumeViewList.ItemRealized += VolumeViewList_ItemRealized_ScrollToCurrentLine;
				}
				//await Task.WhenAll(ViewModel.VolumeList.Select(vvm => vvm.LoadDataAsync(vvm.Id)));

			}
		}

		async void VolumeViewList_ItemRealized_ScrollLoading(object sender, ItemRealizationEventArgs e)
		{
			var vvm = e.Container.DataContext as VolumeViewModel;
			if (e.ItemKind == LongListSelectorItemKind.Item && vvm != null && String.IsNullOrWhiteSpace(vvm.Description) && !vvm.IsLoading)
			{
				Debug.WriteLine("Scroll loading : Volume " + vvm.Id);
				await vvm.LoadDataAsync(vvm.Id);
			}
		}

		private void ChapterBubble_OnTap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var elem = sender as FrameworkElement;
			if (elem == null)
			{
				Debug.WriteLine("Unpredicted Tap sender : " + sender.ToString());
				return;
			}
			var cpvm = elem.DataContext as ViewModels.ChapterPreviewModel;
			if (cpvm == null)
			{
				Debug.WriteLine("Unpredicted Tap sender : " + sender.ToString());
				return;
			}

			var chapterViewUri = new Uri(cpvm.NavigateUri, UriKind.Relative);
			NavigationService.Navigate(chapterViewUri);
		}

		private void VolumeBubble_OnTap(object sender, GestureEventArgs e)
		{
			var elem = sender as FrameworkElement;
			if (elem == null)
			{
				Debug.WriteLine("Unpredicted Tap sender : " + sender.ToString());
				return;
			}
			var vvm = elem.DataContext as ViewModels.VolumeViewModel;
			if (vvm == null)
			{
				Debug.WriteLine("Unpredicted Tap sender : " + sender.ToString());
				return;
			}

			var chapterViewUri = new Uri(vvm.ChapterList[0].NavigateUri, UriKind.Relative);
			NavigationService.Navigate(chapterViewUri);
		}

		private void PinButton_OnTap(object sender, GestureEventArgs e)
		{
			var imageUri = ViewModel.VolumeList[0].CoverImageUri;
			var tileData = new FlipTileData
			{
				SmallBackgroundImage = imageUri,
				BackgroundImage = imageUri,
				Title = ViewModel.Title,
				BackTitle = ViewModel.Title,
				BackContent = ViewModel.VolumeList[0].Description,
			};
			ShellTile.Create(new Uri("/SeriesViewPage.xaml?id=" + ViewModel.Id, UriKind.Relative), tileData, false);
		}
	}
}