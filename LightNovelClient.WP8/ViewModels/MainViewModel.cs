using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using LightNovel.Resources;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Phone.Shell;
using LightNovel.Service;

namespace LightNovel.ViewModels
{
	//using StringCoverGroup = KeyGroup<string, BookCoverViewModel>;

	public class MainViewModel : INotifyPropertyChanged
	{
		public MainViewModel(ApplicationSettings settings)
		{
			//this.Items = new ObservableCollection<ItemViewModel>();
			Settings = settings;
			UserName = Settings.UserName;
			Password = Settings.Password;
			SeriesIndex = null;
			HistoryViewList = null;
			IsIndexDataLoaded = false;
			IsRecentDataLoaded = false;
			IsRecommandLoaded = false;
			IsSignedIn = false;
			IsLoading = false;
			PropertyChanged += MainViewModel_PropertyChanged;
			//using (var reader = new System.IO.StreamReader("SampleData\\5929.txt"))
			//{
			//    Text = reader.ReadToEnd();    
			//}
		}

		async void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsSignedIn")
			{
				if (IsSignedIn)
				{
					await LoadUserFavouriateAsync();
					//await LoadUserRecentAsync();
				} else
				{

				}
			}
		}

		private bool _isDataLoaded;
		private bool _isLoading;
		private bool _isSignedIn;
		private bool _isRecentDataLoaded;
		private string _loadingText;
		private string _userName;
		private string _password; 
		private IList<KeyGroup<string, BookCoverViewModel>> _recommandBookItems;
		private ObservableCollection<HistoryItemViewModel> _historyViewList;
		private ObservableCollection<FavourVolume> _favourList;
		private ObservableCollection<Descriptor> _recentList;
		private Uri _coverBackgroundImageUri;

		public ApplicationSettings Settings { get; set; }

		/// <summary>
		/// A collection for ItemViewModel objects.
		/// </summary>
		//public ObservableCollection<ItemViewModel> Items { get; private set; }
		private List<AlphaKeyGroup<SeriesPreviewModel>> _seriesIndex;
		public List<AlphaKeyGroup<SeriesPreviewModel>> SeriesIndex
		{
			get { return _seriesIndex; }
			set
			{
				if (_seriesIndex == value) return;
				_seriesIndex = value;
				NotifyPropertyChanged();
			}
		}
		public string UserName
		{
			get { return _userName; }
			set
			{
				if (_userName != value)
				{
					_userName = value;
					NotifyPropertyChanged();
				}
			}
		}
		public string Password
		{
			get { return _password; }
			set
			{
				if (_password != value)
				{
					_password = value;
					NotifyPropertyChanged();
				}
			}
		}
		public string LoadingText
		{
			get { return _loadingText; }
			set
			{
				if (_loadingText != value)
				{
					_loadingText = value;
					NotifyPropertyChanged();
				}
			}
		}

		public Uri CoverBackgroundImageUri
		{
			get { return _coverBackgroundImageUri; }
			set
			{
				_coverBackgroundImageUri = value;
				NotifyPropertyChanged();
			}
		}

		public ObservableCollection<HistoryItemViewModel> HistoryViewList
		{
			get { return _historyViewList; }
			set
			{
				if (_historyViewList != value)
				{
					_historyViewList = value;
					NotifyPropertyChanged();
				}
			}
		}
		public ObservableCollection<FavourVolume> FavouriateList
		{
			get { return _favourList; }
			set
			{
				if (_favourList != value)
				{
					_favourList = value;
					NotifyPropertyChanged();
				}
			}
		}
		public ObservableCollection<Descriptor> RecentList
		{
			get { return _recentList; }
			set
			{
				if (_recentList != value)
				{
					_recentList = value;
					NotifyPropertyChanged();
				}
			}
		}

		public IList<KeyGroup<string, BookCoverViewModel>> RecommandBookItems
		{
			get { return _recommandBookItems; }
			set
			{
				if (_recommandBookItems != value)
				{
					_recommandBookItems = value;
					NotifyPropertyChanged();
				}
			}
			
		}

		public bool IsRecentDataLoaded
		{
			get { return _isRecentDataLoaded; }
			set
			{
				_isRecentDataLoaded = value;
				NotifyPropertyChanged();
			}
		}
		public bool IsRecommandLoaded { get; set; }

		public bool IsIndexDataLoaded
		{
			get
			{
				return _isDataLoaded;
			}
			set
			{
				_isDataLoaded = value;
				NotifyPropertyChanged();
			}
		}

		public bool IsSignedIn
		{
			get
			{
				return _isSignedIn;
			}
			set
			{
				_isSignedIn = value;
				NotifyPropertyChanged();
			}
		}

		public bool IsLoading
		{
			get
			{
				return _isLoading;
			}
			set
			{
				_isLoading = value;
				NotifyPropertyChanged();
			}
		}

		/// <summary>
		/// Creates and adds a few ItemViewModel objects into the Items collection.
		/// </summary>
		public async Task LoadSeriesIndexDataAsync()
		{
			if (IsLoading || IsIndexDataLoaded) return;
			LoadingText = "Loading series index data...";
			IsLoading = true;
			try
			{
				var serIndex = await CachedClient.GetSeriesIndexAsync();
				var serVmList = serIndex.Select(series => new SeriesPreviewModel { ID = series.Id, Title = series.Title });
				SeriesIndex = AlphaKeyGroup<SeriesPreviewModel>.CreateGroups(
					serVmList,
					new System.Globalization.CultureInfo("zh-CN"),
					svm => svm.Title, true);
				IsIndexDataLoaded = true;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "Data error", MessageBoxButton.OK);
			}
			IsLoading = false;
		}

		public async Task LoadRecommandDataAsync()
		{
			if (IsLoading || IsRecommandLoaded) return;
			LoadingText = "Loading recommand books";
			IsLoading = true;
			try
			{
				var recommandBookGroups = await CachedClient.GetRecommandedBookLists();
				RecommandBookItems = new List<KeyGroup<string, BookCoverViewModel>>();
				foreach (var bookGroup in recommandBookGroups)
				{
					var group = new KeyGroup<string, BookCoverViewModel>
					{
						Key = bookGroup.Key
					};
					if (bookGroup.Value.Count <= 12)
						group.AddRange(bookGroup.Value.Select(x=>new BookCoverViewModel(x)));
					else
						group.AddRange(bookGroup.Value.Take(12).Select(x=>new BookCoverViewModel(x)));
					RecommandBookItems.Add(group);
				}
				IsLoading = false;
				IsRecommandLoaded = true;
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message, "Exception when retriving recommanded books", MessageBoxButton.OK);
				IsLoading = false;
			}

		}

		public async Task LoadUserFavouriateAsync()
		{
			if (!IsLoading && (FavouriateList == null) && IsSignedIn)
			{
				LoadingText = "Loading user favouriates...";
				IsLoading = true;

				try
				{
					FavouriateList = new ObservableCollection<FavourVolume>();
					var favList = await LightKindomHtmlClient.GetUserFavoriteVolumesAsync();
					foreach (var item in favList)
					{
						FavouriateList.Add(item);
					}
				}
				catch (Exception)
				{
					FavouriateList = null;
					MessageBox.Show("Load User favouriate failed.");
				}

				IsLoading = false;
				LoadingText = "";
			}
		}
		public async Task LoadUserRecentAsync()
		{
			if (!IsLoading && !IsUserRecentLoaded && IsSignedIn)
			{
				LoadingText = "Loading user recent...";
				IsLoading = true;

				try
				{
					RecentList = new ObservableCollection<Descriptor>();
					var recentList = (await LightKindomHtmlClient.GetUserRecentViewedVolumesAsync()).ToList();
					foreach (var item in recentList)
					{
						RecentList.Add(item);
					}
				}
				catch (Exception)
				{
					RecentList = null;
					MessageBox.Show("Load User recent failed.");
				}

				IsLoading = false;
				LoadingText = "";
			}
		}

		public async Task UpdateRecentViewAsync()
		{
			if (IsLoading || !App.IsHistoryListChanged)
				return;
			LoadingText = "updating recent data...";
			IsLoading = true;
			if (HistoryViewList == null)
				HistoryViewList = new ObservableCollection<HistoryItemViewModel>();
			if (App.HistoryList == null)
				await App.LoadHistoryDataAsync();
			var historyList = App.HistoryList;

			HistoryViewList.Clear();
			for (int idx = historyList.Count - 1; idx >= 0; idx--)
			{
				var item = historyList[idx];
				var hvm = new HistoryItemViewModel
				{
					Position = item.Position,
					ProgressPercentage = item.Progress,
					CoverImageUri = item.DescriptionImageUri,
					Description = item.ContentDescription,
					ChapterTitle = item.ChapterTitle,
					VolumeTitle = item.VolumeTitle,
					SeriesTitle = item.SeriesTitle,
					UpdateTime = item.ViewDate
				};
				HistoryViewList.Add(hvm);

				//if (String.IsNullOrWhiteSpace(item.Position.VolumeId)) continue;
				//var series = await LightKindomHtmlClient.GetSeriesAsync(item.Position.SeriesId);
				////var volume = await LightKindomHtmlClient.GetVolumeAsync(item.Position.VolumeId);
				//var volume = series.Volumes.First(vol => vol.Id == item.Position.VolumeId);
				//hvm.SeriesTitle = series.Title;
				//hvm.CoverImageUri = volume.CoverImageUri;
				//hvm.VolumeTitle = volume.Title;
				//hvm.ChapterTitle = volume.ChapterList[item.Position.ChapterNumber];
			}

			var mainTile = ShellTile.ActiveTiles.FirstOrDefault();
			if (mainTile != null && historyList.Count > 0)
			{
				var latestItem = historyList[historyList.Count - 1];
				var imageUri = new Uri(latestItem.DescriptionImageUri);
				var data = new FlipTileData
				{
					SmallBackgroundImage = imageUri,
					BackgroundImage = imageUri,
					Title = "Light Novel",
					BackTitle = latestItem.VolumeTitle,
					BackContent = latestItem.ContentDescription,
				};
				mainTile.Update(data);
				CoverBackgroundImageUri = imageUri;
			}
			App.IsHistoryListChanged = false;
			IsLoading = false;
			IsRecentDataLoaded = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		internal void Logout()
		{
			if (!IsSignedIn)
				return;
			LightKindomHtmlClient.Logout();
			UserName = "";
			Password = "";
			Settings.UserName = UserName;
			Settings.Password = Password;
			Settings.Credential = null;
			IsSignedIn = false;

		}

		internal async Task TryLoginAsync()
		{
			if (IsSignedIn || String.IsNullOrWhiteSpace(UserName) || String.IsNullOrWhiteSpace(Password))
				return;
			var session = App.Settings.Credential;
			if (session != null && !session.Expired) // Login with credential cookie
			{
				LightKindomHtmlClient.Credential = session;
				IsSignedIn = true;
				return;
			}
			IsLoading = true;
			LoadingText = "Logging into Light Kindom";
			try
			{
				var credential = await LightKindomHtmlClient.LoginAsync(UserName, Password);
				IsSignedIn = true;
				App.Settings.UserName = UserName;
				App.Settings.Password = Password;
				App.Settings.Credential = credential;
			}
			catch (Exception exception)
			{
				MessageBox.Show("Login failed, please check your network and username / password.");
			}
			IsLoading = false;
			LoadingText = "";
		}

		public bool IsFavoriteLoaded
		{
			get
			{
				return FavouriateList != null;
			}
		}
		public bool IsUserRecentLoaded
		{
			get
			{
				return RecentList != null;
			}
		}
	}
}