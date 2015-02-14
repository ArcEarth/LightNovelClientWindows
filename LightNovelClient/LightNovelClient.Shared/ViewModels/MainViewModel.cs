using LightNovel.Common;
using LightNovel.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace LightNovel.ViewModels
{
	//using StringCoverGroup = KeyGroup<string, BookCoverViewModel>;
	public class FavoriteSectionViewModel : ObservableCollection<HistoryItemViewModel>, INotifyPropertyChanged
	{
		public bool IsEmpty
		{
			get { return base.Count == 0; }
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

		protected override event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public async Task LoadAsync(bool foreceRefresh = false, int maxItemCount = 9)
		{
			if (IsLoading) return;
			IsLoading = true;
			try
			{
				await App.Current.PullBookmarkFromUserFavoriteAsync(foreceRefresh);
			}
			catch (Exception exception)
			{
				Debug.WriteLine(exception.Message);
				IsLoading = false;
				IsLoaded = false;
				return;
			}

			this.Clear();
			foreach (var bk in App.Current.BookmarkList)
			{
				var hvm = new HistoryItemViewModel
				{
					Position = bk.Position,
					ProgressPercentage = bk.Progress,
					CoverImageUri = bk.DescriptionImageUri,
					Description = bk.ContentDescription,
					ChapterTitle = bk.ChapterTitle,
					VolumeTitle = bk.VolumeTitle,
					SeriesTitle = bk.SeriesTitle,
					UpdateTime = bk.ViewDate
				};

				if (!String.IsNullOrEmpty(bk.DescriptionThumbnailUri))
					hvm.CoverImageUri = bk.DescriptionThumbnailUri;
				this.Add(hvm);
				NotifyPropertyChanged("IsEmpty");
				if (String.IsNullOrEmpty(bk.Position.SeriesId))
				{
					var volume = await CachedClient.GetVolumeAsync(bk.Position.VolumeId);
					hvm.CoverImageUri = volume.CoverImageUri;
					hvm.Description = volume.Description;
					bk.Position.SeriesId = volume.ParentSeriesId;
					bk.ContentDescription = volume.Description;
					bk.DescriptionThumbnailUri = volume.CoverImageUri;
				}
			}
			NotifyPropertyChanged("IsEmpty");
			await App.Current.SaveBookmarkDataAsync();

			IsLoaded = true;
			IsLoading = false;

			//await App.Current.User.SyncFavoriteListAsync(foreceRefresh);

			//App.Current.User.FavoriteList.CollectionChanged += FavoriteList_CollectionChanged;

			//var favList = from fav in App.Current.User.FavoriteList orderby fav.VolumeNo group fav by fav.SeriesTitle ;

			//foreach (var series in favList)
			//{
			//	var vol = series.LastOrDefault();
			//	var item = new HistoryItemViewModel() { SeriesTitle = vol.SeriesTitle, VolumeTitle = vol.VolumeTitle, UpdateTime = vol.FavTime};
			//	var volume = await CachedClient.GetVolumeAsync(vol.VolumeId);
			//	item.CoverImageUri = volume.CoverImageUri;
			//	item.Description = volume.Description;
			//	item.Position = new NovelPositionIdentifier { SeriesId = volume.ParentSeriesId, VolumeId = volume.Id, VolumeNo = -1 };
			//	this.Add(item);
			//	NotifyPropertyChanged("IsEmpty");
			//}

			//IsLoading = false;
			//IsLoaded = true;
			//return App.Current.User.FavoriteList;

		}

		private async void FavoriteList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var FavoriteList = sender as ObservableCollection<FavourVolume>;
			switch (e.Action)
			{
				case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
					foreach (FavourVolume vol in e.NewItems)
					{
						var serp = this.FirstOrDefault(ser => ser.SeriesTitle == vol.SeriesTitle);
						if (serp == null)
						{
							var item = new HistoryItemViewModel() { SeriesTitle = vol.SeriesTitle, VolumeTitle = vol.VolumeTitle, UpdateTime = vol.FavTime };
							var volume = await CachedClient.GetVolumeAsync(vol.VolumeId);
							item.CoverImageUri = volume.CoverImageUri;
							item.Description = volume.Description;
							item.Position = new NovelPositionIdentifier { SeriesId = volume.ParentSeriesId, VolumeId = volume.Id, VolumeNo = -1 };
							this.Add(item);
							NotifyPropertyChanged("IsEmpty");
						}
						else if (int.Parse(serp.Position.VolumeId) < int.Parse(vol.VolumeId))
						{
							serp.SeriesTitle = vol.SeriesTitle;
							serp.VolumeTitle = vol.VolumeTitle;
							serp.UpdateTime = vol.FavTime;
							var volume = await CachedClient.GetVolumeAsync(vol.VolumeId);
							serp.CoverImageUri = volume.CoverImageUri;
							serp.Description = volume.Description;
							serp.Position = new NovelPositionIdentifier { SeriesId = volume.ParentSeriesId, VolumeId = volume.Id, VolumeNo = -1 };
						}
					}
					break;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
					foreach (FavourVolume vol in e.OldItems)
					{
						var serp = this.FirstOrDefault(ser => ser.SeriesTitle == vol.SeriesTitle);
						if (serp == null)
							continue;
						if (!FavoriteList.Any(f => f.SeriesTitle == serp.SeriesTitle))
						{
							this.Remove(serp);
						}
					}
					break;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
					IsLoading = true;
					var favList = from fav in FavoriteList orderby fav.VolumeNo group fav by fav.SeriesTitle;
					this.Clear();
					foreach (var series in favList)
					{
						var vol = series.LastOrDefault();
						var item = new HistoryItemViewModel() { SeriesTitle = vol.SeriesTitle, VolumeTitle = vol.VolumeTitle, UpdateTime = vol.FavTime };
						var volume = await CachedClient.GetVolumeAsync(vol.VolumeId);
						item.CoverImageUri = volume.CoverImageUri;
						item.Description = volume.Description;
						item.Position = new NovelPositionIdentifier { SeriesId = volume.ParentSeriesId, VolumeId = volume.Id, VolumeNo = -1 };
						this.Add(item);
						NotifyPropertyChanged("IsEmpty");
					}
					break;
					IsLoading = false;
				default:
					break;
			}
		}
	}

	public class RecentSectionViewModel : ObservableCollection<HistoryItemViewModel>
	{
		public bool IsEmpty
		{
			get { return base.Count == 0; }
		}

		private bool _isLoaded = false;
		private bool _isLoading = false;
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

		protected override event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public async Task LoadOnlineAsync()
		{
			if (!IsLoading && !IsLoaded)
			{
				IsLoading = true;

				try
				{
					var recentList = await LightKindomHtmlClient.GetUserRecentViewedVolumesAsync();
					if (recentList != null)
					{
						this.Clear();
						foreach (var item in recentList)
						{
							var hvm = new HistoryItemViewModel
							{
								Position = new NovelPositionIdentifier
								{
									VolumeId = item.Id,

								},
								SeriesTitle = item.Title,
							};
							this.Add(hvm);
						}
					}
				}
				catch (Exception exception)
				{
					throw exception;
					//MessageBox.Show("Load User recent failed.");
				}
				NotifyPropertyChanged("IsEmpty");
				IsLoading = false;
			}
		}

		public async Task LoadLocalAsync(bool SkipLatest = false)
		{
			if (IsLoading)
				return;
			IsLoading = true;
			if (App.Current.RecentList == null)
				await App.Current.LoadHistoryDataAsync();
			var historyList = App.Current.RecentList;

			this.Clear();

			int begin = historyList.Count - 1;
			if (SkipLatest)
				--begin;
			for (int idx = begin; idx >= 0; idx--)
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
				if (!String.IsNullOrEmpty(item.DescriptionThumbnailUri))
					hvm.CoverImageUri = item.DescriptionThumbnailUri;
				this.Add(hvm);
				if (this.Count >= 10) // Load the first 10th to show
					break;
			}
			App.Current.IsHistoryListChanged = false;
			NotifyPropertyChanged("IsEmpty");
			IsLoading = false;
			IsLoaded = true;
		}

	}

	public class RecommandSectionViewModel : ObservableCollection<KeyGroup<string, BookCoverViewModel>>, INotifyPropertyChanged
	{
		private bool _isLoaded;
		private bool _isLoading;
		public bool IsEmpty
		{
			get { return base.Count == 0; }
		}
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

		protected override event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public void Load(IList<KeyValuePair<string, IList<BookItem>>> recommandBookGroups)
		{
			foreach (var bookGroup in recommandBookGroups)
			{
				var group = new KeyGroup<string, BookCoverViewModel>
				{
					Key = bookGroup.Key
				};
				group.AddRange(bookGroup.Value.Select(x => new BookCoverViewModel(x)));
				this.Add(group);
			}
			NotifyPropertyChanged("IsEmpty");
			IsLoaded = true;
			IsLoading = false;
		}

		public async Task<IList<KeyValuePair<string, IList<BookItem>>>> LoadAsync(int maxVolumeCount = 9)
		{
			if (!IsLoading && !IsLoaded)
			{
				IsLoading = true;
				try
				{
					var recommandBookGroups = await CachedClient.GetRecommandedBookLists();
					foreach (var bookGroup in recommandBookGroups)
					{
						var group = new KeyGroup<string, BookCoverViewModel>
						{
							Key = bookGroup.Key
						};
						if (bookGroup.Value.Count <= maxVolumeCount)
							group.AddRange(bookGroup.Value.Select(x => new BookCoverViewModel(x)));
						else
							group.AddRange(bookGroup.Value.Take(maxVolumeCount).Select(x => new BookCoverViewModel(x)));
						this.Add(group);
					}
					IsLoading = false;
					IsLoaded = true;
					NotifyPropertyChanged("IsEmpty");
					return recommandBookGroups;
				}
				catch (Exception exception)
				{
					IsLoading = false;
					IsLoaded = false;
					Debug.WriteLine(exception.Message);
					return null;
				}

			}
			return null;
		}

	}

	public class MainViewModel : INotifyPropertyChanged
	{
		public MainViewModel()
		{
			//this.Items = new ObservableCollection<ItemViewModel>();
			Settings = null;
			//UserName = Settings.UserName;
			//Password = Settings.Password;
			SeriesIndex = null;
			IsIndexDataLoaded = false;
			IsRecentDataLoaded = false;
			IsRecommandLoaded = false;
			IsSignedIn = false;
			IsLoading = false;
			//PropertyChanged += MainViewModel_PropertyChanged;
			//using (var reader = new System.IO.StreamReader("SampleData\\5929.txt"))
			//{
			//    Text = reader.ReadToEnd();    
			//}
		}

		public bool IsOnline
		{
			get { return App.IsConnectedToInternet(); }
		}

		//async void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == "IsSignedIn")
		//	{
		//		if (IsSignedIn)
		//		{
		//			//await LoadUserFavouriateAsync();
		//			//await LoadUserRecentAsync();
		//		} else
		//		{

		//		}
		//	}
		//}

		private bool _isDataLoaded;
		private bool _isLoading;
		private bool _isSignedIn;
		private bool _isRecentDataLoaded;
		private string _loadingText;
		private string _userName;
		private string _password;
		private HistoryItemViewModel _lastReadSection;
		private Uri _coverBackgroundImageUri;

		public ApplicationSettings Settings { get; set; }

		private RecommandSectionViewModel _recommandSection = new RecommandSectionViewModel();
		public RecommandSectionViewModel RecommandSection { get { return _recommandSection; } }

		private FavoriteSectionViewModel _favoriteSection = new FavoriteSectionViewModel();
		public FavoriteSectionViewModel FavoriteSection { get { return _favoriteSection; } }

		private RecentSectionViewModel _recentSection = new RecentSectionViewModel();
		public RecentSectionViewModel RecentSection { get { return _recentSection; } }
		/// <summary>
		/// A collection for ItemViewModel objects.
		/// </summary>
		//public ObservableCollection<ItemViewModel> Items { get; private set; }
		private IList<IGrouping<string, Descriptor>> _seriesIndex;
		public IList<IGrouping<string, Descriptor>> SeriesIndex
		{
			get { return _seriesIndex; }
			set
			{
				if (_seriesIndex == value) return;
				_seriesIndex = value;
				NotifyPropertyChanged();
			}
		}

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
		public HistoryItemViewModel LastReadSection
		{
			get { return _lastReadSection; }
			set
			{
				if (_lastReadSection != value)
				{
					_lastReadSection = value;
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
			if (IsIndexDataLoaded) return;
			LoadingText = "Loading series index data...";
			IsLoading = true;
			try
			{
				var serIndex = await CachedClient.GetSeriesIndexAsync();
				//var serVmList = serIndex.Select(series => new SeriesPreviewModel { ID = series.Id, Title = series.Title });
				var cgs = new Windows.Globalization.Collation.CharacterGroupings();
				SeriesIndex = (from series in serIndex
							   group series
							   by cgs.Lookup(series.Title) into g
							   orderby g.Key
							   select g).ToList();

				//SeriesIndex = AlphaKeyGroup<SeriesPreviewModel>.CreateGroups(
				//	serVmList,
				//	new System.Globalization.CultureInfo("zh-CN"),
				//	svm => svm.Title, true);
				IsIndexDataLoaded = true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Exception when retrieving series index : " + exception.Message);
				//throw exception;
				//MessageBox.Show(exception.Message, "Data error", MessageBoxButton.OK);
			}
			IsLoading = false;
		}

		//public async Task LoadUserRecentAsync()
		//{
		//	if (!IsLoading && !IsUserRecentLoaded && IsSignedIn)
		//	{
		//		LoadingText = "Loading user recent...";
		//		IsLoading = true;

		//		if (RecentList == null)
		//			RecentList = new ObservableCollection<Descriptor>();
		//		try
		//		{
		//			RecentList.Clear();
		//			var recentList = (await LightKindomHtmlClient.GetUserRecentViewedVolumesAsync()).ToList();
		//			foreach (var item in recentList)
		//			{
		//				RecentList.Add(item);
		//			}
		//		}
		//		catch (Exception exception)
		//		{
		//			Debug.WriteLine("Load User recent failed : " + exception.Message);
		//			//MessageBox.Show("Load User recent failed.");
		//		}

		//		IsLoading = false;
		//		LoadingText = "";
		//	}
		//}

		//public async Task UpdateRecentViewAsync()
		//{
		//	if (IsLoading || !App.Current.IsHistoryListChanged)
		//		return;
		//	LoadingText = "updating recent data...";
		//	IsLoading = true;
		//	if (HistoryViewList == null)
		//		HistoryViewList = new ObservableCollection<HistoryItemViewModel>();
		//	if (App.Current.RecentList == null)
		//		await App.Current.LoadHistoryDataAsync();
		//	var historyList = App.Current.RecentList;

		//	HistoryViewList.Clear();
		//	for (int idx = historyList.Count - 1; idx >= 0; idx--)
		//	{
		//		var item = historyList[idx];
		//		var hvm = new HistoryItemViewModel
		//		{
		//			Position = item.Position,
		//			ProgressPercentage = item.Progress,
		//			CoverImageUri = item.DescriptionImageUri,
		//			Description = item.ContentDescription,
		//			ChapterTitle = item.ChapterTitle,
		//			VolumeTitle = item.VolumeTitle,
		//			SeriesTitle = item.SeriesTitle,
		//			UpdateTime = item.ViewDate
		//		};
		//		HistoryViewList.Add(hvm);
		//	}
		//	//Windows.UI.StartScreen.SecondaryTile tile;
		//	//var mainTile = ShellTile.ActiveTiles.FirstOrDefault();
		//	//if (mainTile != null && historyList.Count > 0)
		//	//{
		//	//	var latestItem = historyList[historyList.Count - 1];
		//	//	var imageUri = new Uri(latestItem.DescriptionImageUri);
		//	//	var data = new FlipTileData
		//	//	{
		//	//		SmallBackgroundImage = imageUri,
		//	//		BackgroundImage = imageUri,
		//	//		Title = "Light Novel",
		//	//		BackTitle = latestItem.VolumeTitle,
		//	//		BackContent = latestItem.ContentDescription,
		//	//	};
		//	//	mainTile.Update(data);
		//	//	CoverBackgroundImageUri = imageUri;
		//	//}
		//	App.Current.IsHistoryListChanged = false;
		//	IsLoading = false;
		//	IsRecentDataLoaded = true;
		//}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		internal async Task LogOutAsync()
		{
			if (!IsSignedIn)
				return;
			IsLoading = true;
			try
			{
				await App.Current.SignOutAsync();
			}
			catch
			{

			}
			UserName = null;
			Password = null;
			IsSignedIn = false;
			FavoriteSection.Clear(); //Erase the user favorite data
			FavoriteSection.IsLoaded = false;
			FavoriteSection.NotifyPropertyChanged("IsEmpty");
			IsLoading = false;
		}

		internal async Task<bool> TryLogInWithStoredCredentialAsync()
		{
			if (!IsOnline)
				return false;
			if (App.Current.IsSignedIn)
			{
				IsSignedIn = true;
				UserName = App.Current.User.UserName;
			}
			else
			{
				UserName = "Signing in...";
				IsSignedIn = await App.Current.SignInAutomaticllyAsync();
				if (IsSignedIn)
					UserName = App.Current.User.UserName;
				else
					UserName = "Tap to Sign in";
			}
			return IsSignedIn;
		}

		internal async Task<bool> TryLogInWithUserInputCredentialAsync()
		{
			if (IsSignedIn)
				return true;
			if (String.IsNullOrWhiteSpace(UserName) || String.IsNullOrWhiteSpace(Password))
				return false;
			try
			{
				IsSignedIn = await App.Current.SignInAsync(UserName, Password) != null;
			}
			catch (Exception)
			{
				IsSignedIn = false;
				App.Current.User = null;
			}
			if (IsSignedIn)
			{
				UserName = App.Current.User.UserName;
			}
			return IsSignedIn;
		}
	}
}