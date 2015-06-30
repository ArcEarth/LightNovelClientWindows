using LightNovel.Common;
using LightNovel.Service;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.StartScreen;
using Windows.UI.Xaml.Media;

namespace LightNovel.ViewModels
{
	//[NotifyPropertyChanged]
	public class PropertyChangingEventArgs : EventArgs
	{
		public PropertyChangingEventArgs(string name, object oldValue, object newValue)
		{

		}
		// Summary:
		//     Gets the name of the property that changed.
		//
		// Returns:
		//     The name of the property that changed.
		//public string PropertyName { get; }

		//public object OldValue { get; }
		//public object NewValue { get; }
	}

	public enum PreCachePolicy
	{
		DoNotCache = 0,
		CachePrev = -1,
		CacheNext = 1,
	}

	public class ReadingPageViewModel : INotifyPropertyChanged
	{
		string LoadingHeader;
		string LoadingFailedContentNetworkIssue;
		string LoadingFailedContentSeviceIssue;
		string LoadingFailedContentOverallLabel;

		public ReadingPageViewModel()
		{

			//_FontSize = (double)App.Current.Resources["AppFontSizeMediumLarge"];
			//_Background = (SolidColorBrush)App.Current.Resources["AppReadingBackgroundBrush"];
			//_Foreground = (SolidColorBrush)App.Current.Resources["AppForegroundBrush"];
			_FontSize = AppGlobal.Settings.FontSize;
			_Background = AppGlobal.Settings.Background;
			_Foreground = AppGlobal.Settings.Foreground;

			var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
			LoadingHeader = resourceLoader.GetString("LoadingHeader");
			LoadingFailedContentOverallLabel = resourceLoader.GetString("LoadingFailedContentOverallLabel");
			LoadingFailedContentSeviceIssue = resourceLoader.GetString("LoadingFailedContentSeviceIssue");
			LoadingFailedContentNetworkIssue = resourceLoader.GetString("LoadingFailedContentNetworkIssue");

			//this.PropertyChanged += ReadingPageViewModel_PropertyChanged;
		}

		//protected override bool HasMoreItemsOverride()
		//{ 
		//		if (ChapterNo < 0 || VolumeData == null)
		//			return false;
		//		return ChapterNo < VolumeData.Chapters.Count - 1;
		//}

		//protected override async Task<IEnumerable<LineViewModel>> LoadMoreItemsOverrideAsync(CancellationToken c, uint count)
		//{
		//	var NextChapterData = await CachedClient.GetChapterAsync(VolumeData.Chapters[ChapterNo + 1].Id);
		//	return _ChapterData.Lines.Select(line => new LineViewModel(line, ChapterData.Id));
		//}


		async void ReadingPageViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "SeriesId":
					if (SeriesData == null || SeriesId != int.Parse(SeriesData.Id))
					{
						SeriesData = await CachedClient.GetSeriesAsync(SeriesId.ToString());
					}
					break;
				case "VolumeNo":
					if (VolumeData == null || VolumeData.Id != SeriesData.Volumes[VolumeNo].Id)
						VolumeData = SeriesData.Volumes[VolumeNo] = await CachedClient.GetVolumeAsync(SeriesData.Volumes[VolumeNo].Id);
					break;
				case "ChapterNo":
					if (ChapterData == null || ChapterData.Id != VolumeData.Chapters[ChapterNo].Id)
					{
						ChapterData = await CachedClient.GetChapterAsync(VolumeData.Chapters[ChapterNo].Id);
						//VolumeData.Chapters[ChapterNo];
						//CommentIndex = (await LightKindomHtmlClient.GetCommentedLinesListAsync(ChapterNo.ToString())).ToList();
					}
					break;
				case "LineNo":
					break;
				case "PageNo":
					break;
				case "Contents":
					break;
				case "Index":
					break;
				default:
					break;
			}
		}

		public event EventHandler<IEnumerable<int>> CommentsListLoaded;
		//[IgnoreAutoChangeNotification]
		private Series _SeriesData;
		private Volume _VolumeData;
		private Chapter _ChapterData;
		public Series SeriesData
		{
			get
			{
				return _SeriesData;
			}
			private set
			{
				if (_SeriesData != value)
				{
					_SeriesData = value;
					//Index = _SeriesData.Volumes;
					Index = (from vol in _SeriesData.Volumes
							 select new VolumeViewModel
							 {
								 Title = vol.Title,
								 Label = vol.Label,
								 Description = vol.Description,
								 Author = vol.Author,
								 CoverImageUri = new Uri(vol.CoverImageUri),
								 Chapters = vol.Chapters.Select(chapter => new ChapterPreviewModel
								 {
									 No = chapter.ChapterNo,
									 Id = chapter.Id,
									 Title = chapter.Title,
									 VolumeNo = vol.VolumeNo,
								 }).ToList()
							 }).ToList();


					if (VolumeNo >= 0 && ChapterNo >= 0 && Index.Count > VolumeNo && Index[VolumeNo].Chapters.Count > ChapterNo)
					{
						SeconderyIndex = Index[VolumeNo].Chapters;
					}

					NotifyPropertyChanged("Header");
				}
			}
		}
		//[IgnoreAutoChangeNotification]
		public Volume VolumeData
		{
			get
			{
				return _VolumeData;
			}
			private set
			{
				if (_VolumeData != value)
				{
					_VolumeData = value;
					NotifyPropertyChanged("Header");
					NotifyPropertyChanged("IsFavored");
					NotifyPropertyChanged("IsPinned");

				}
			}
		}

		//[IgnoreAutoChangeNotification]
		public Chapter ChapterData
		{
			get
			{
				return _ChapterData;
			}
			private set
			{
				if (_ChapterData != value)
				{
					_ChapterData = value;
					NotifyPropertyChanged("Header");
					NotifyPropertyChanged("ChapterHeader");
					NotifyPropertyChanged("NextChapterHeader");
				}
			}
		}

		public String Header
		{
			get
			{
#if WINDOWS_APP // USE Long header
				if (SeriesData != null && VolumeData != null && ChapterData != null)
					return SeriesData.Title + " / " + VolumeData.Title + " / " + ChapterData.Title;
				else
					return LoadingHeader;
#else // WINDOWS_PHONE_APP
				if (SeriesData != null && VolumeData != null && ChapterData != null)
					return SeriesData.Title;
				else
					return LoadingHeader;
#endif
			}
		}

		public string ChapterHeader
		{
			get
			{
				if (ChapterData != null)
					return ChapterData.Title;
				else
					return "Loading...";
			}
		}

		public string NextChapterHeader
		{
			get
			{
				if (HasNext)
				{
					return Index[VolumeNo][ChapterNo + 1].Title;
				} else
				{
					return String.Empty;
				}
			}
		}

		private bool _IsFullScreen = false;
		private bool _IsLoading = false;
		private int _SeriesId = -1;
		private int _VolumeNo = -1;
		private int _ChapterNo = -1;
		private int _LineNo = -1;
		private int _PageNo = -1;
		private int _PagesCount = -1;
		private IList _Contents;
		private IList<VolumeViewModel> _Index;
		private IList<ChapterPreviewModel> _SeconderyIndex;

		public bool IsLoading
		{
			get { return _IsLoading; }
			set
			{
				if (_IsLoading != value)
				{
					_IsLoading = value;
					NotifyPropertyChanged();
				}
			}
		}

		public bool IsFullScreen
		{
			get { return _IsFullScreen; }
			set
			{
				if (_IsFullScreen != value)
				{
					_IsFullScreen = value;
					NotifyPropertyChanged();
				}
			}
		}

        public bool IsSignedIn => AppGlobal.IsSignedIn;

        public int SeriesId
		{
			get { return _SeriesId; }
			private set
			{
				if (_SeriesId != value)
				{
					_SeriesId = value;
					if (_SeriesId > 0 && (SeriesData == null || SeriesId != int.Parse(SeriesData.Id)))
					{
						throw new NotImplementedException("Set SeriesData before set series ID");
					}
					NotifyPropertyChanged();
				}
			}
		}

		public int VolumeNo
		{
			get { return _VolumeNo; }
			private set
			{
				if (_VolumeNo != value)
				{
					_VolumeNo = value;
					if (_VolumeNo > 0 && (VolumeData == null || VolumeData.Id != SeriesData.Volumes[VolumeNo].Id))
					{
						throw new NotImplementedException("Set VolumeData before set series ID");
					}
					NotifyPropertyChanged();
					NotifyPropertyChanged("NextChapterHeader");

                    if (Index != null && _VolumeNo >=0 && _ChapterNo >= 0)
						SeconderyIndex = Index[_VolumeNo].Chapters;

				}
			}
		}

		public int ChapterNo
		{
			get { return _ChapterNo; }
			private set
			{
				if (_ChapterNo != value)
				{
					_ChapterNo = value;

					if (_ChapterNo > 0 && (ChapterData == null || ChapterData.Id != VolumeData.Chapters[ChapterNo].Id))
					{
						throw new NotImplementedException("Set ChapterData before set series ID");
					}

                    if (Index != null && _VolumeNo >=0 && _ChapterNo >= 0)
						SeconderyIndex = Index[_VolumeNo].Chapters;

					var cvm = Index[VolumeNo][ChapterNo];
					cvm.NotifyPropertyChanged("IsDownloaded");
					NotifyPropertyChanged();
					NotifyPropertyChanged("HasPrev");
					NotifyPropertyChanged("HasNext");
					NotifyPropertyChanged("NextChapterHeader");
					NotifyPropertyChanged("IsDownloaded");
				}
			}
		}

		public int LineNo
		{
			get { return _LineNo; }
			set
			{
				if (_LineNo != value)
				{
					_LineNo = value;
					SuppressViewChange = false;
					NotifyPropertyChanged();
				}
			}
		}

		public int PageNo
		{
			get { return _PageNo; }
			set
			{
				if (_PageNo != value)
				{
					_PageNo = value;
					SuppressViewChange = false;
					NotifyPropertyChanged();
				}
			}
		}
		public bool HasPrev
		{
			get
			{
				return (SeriesData != null) && (_ChapterNo > 0);
			}
		}
		public bool HasNext
		{
			get
			{
				return (SeriesData != null) && (VolumeData != null) && (_ChapterNo + 1 < VolumeData.Chapters.Count); ;
			}
		}
		public bool EnableComments
		{
			get
			{
				return AppGlobal.Settings.EnableComments;
			}
			set
			{
                AppGlobal.Settings.EnableComments = value;
				NotifyPropertyChanged();
			}
		}

		public int PagesCount
		{
			get { return _PagesCount; }
			set
			{
				if (_PagesCount != value)
				{
					_PagesCount = value;
					NotifyPropertyChanged();
				}
			}
		}

		public IList Contents
		{
			get { return _Contents; }
			private set
			{
				if (_Contents != value)
				{
					_Contents = value;
					NotifyPropertyChanged();
				}
			}
		}

		public IList<VolumeViewModel> Index
		{
			get { return _Index; }
			private set
			{
				if (_Index != value)
				{
					_Index = value;
					NotifyPropertyChanged();
				}
			}
		}

        public IList<ChapterPreviewModel> SeconderyIndex
		{
			get { return _SeconderyIndex; }
			private set
			{
				if (_SeconderyIndex != value)
				{
					_SeconderyIndex = value;
					NotifyPropertyChanged();
				}
			}
		}

		public bool IsPinned
		{
			get
			{
				if (SeriesId == 0)
					return false;
				return SecondaryTile.Exists(SeriesId.ToString());
			}
		}
		//public bool IsCached
		//{
		//	get
		//	{
		//		return false;
		//	}
		//}

		public bool IsFavored
		{
			get
			{
				//if (Index == null || !App.Current.IsSignedIn || App.User == null || App.User.FavoriteList == null)
				//	return false;
				//return App.User.FavoriteList.Any(vol => vol.VolumeId == VolumeData.Id);
				if (Index == null || AppGlobal.BookmarkList == null)
					return false;
				return AppGlobal.BookmarkList.Any(bk => (bk.SeriesTitle == SeriesData.Title || bk.Position.SeriesId == SeriesData.Id));
			}
		}

		public bool IsDownloaded
		{
			get
			{
				for (int i = ChapterNo + 2; i < VolumeData.Chapters.Count; i++)
				{
					if (!CachedClient.IsChapterCached( VolumeData.Chapters[i].Id))
						return false;
				}
				for (int i = VolumeNo + 1; i < SeriesData.Volumes.Count; i++)
				{
					if (!SeriesData.Volumes[i].Chapters.All(c => CachedClient.IsChapterCached(c.Id)))
						return false;
				}
				return true;
			}
		}

		public async Task<bool> AddCurrentVolumeToFavoriteAsync()
		{
			var bookmark = CreateBookmark();
			var result = await AddOrUpdateBookmark(bookmark);
			NotifyPropertyChanged("IsFavored");
			return result;
		}

		public async Task<bool> AddOrUpdateBookmark(BookmarkInfo bookmark)
		{
            AppGlobal.BookmarkList.RemoveAll(bk => bk.Position.SeriesId == bookmark.Position.SeriesId || bookmark.SeriesTitle == bk.SeriesTitle);
			AppGlobal.BookmarkList.Add(bookmark);
			await AppGlobal.SaveBookmarkDataAsync();
			//NotifyPropertyChanged("IsFavored");
			if (AppGlobal.IsSignedIn)
			{
				var result = await AppGlobal.User.AddUserFavriteAsync(VolumeData, _SeriesData.Title);
				return result;
			}
			return true;
		}

		public async Task<bool> RemoveCurrentVolumeFromFavoriteAsync()
		{
			AppGlobal.BookmarkList.RemoveAll(bk => (bk.SeriesTitle == SeriesData.Title || bk.Position.SeriesId == SeriesData.Id));
			await AppGlobal.SaveBookmarkDataAsync(); 
			NotifyPropertyChanged("IsFavored");
			if (AppGlobal.IsSignedIn)
			{
				var favol = AppGlobal.User.FavoriteList.FirstOrDefault(fav => fav.VolumeId == VolumeData.Id);
				if (favol == null)
					return true;
				var favDeSer = (from fav in AppGlobal.User.FavoriteList where fav.SeriesTitle == SeriesData.Title select fav.FavId).ToArray();
				if (favDeSer.Any(id => id == null))
				{
					await AppGlobal.User.SyncFavoriteListAsync(true);
					(from fav in AppGlobal.User.FavoriteList where fav.SeriesTitle == SeriesData.Title select fav.FavId).ToArray();
				}
				await AppGlobal.User.RemoveUserFavriteAsync(favDeSer);
			}
			return true;
		}

		public Task<IEnumerable<string>> GetComentsAsync(int LineNo)
		{
			return LightKindomHtmlClient.GetCommentsAsync(LineNo.ToString(), _ChapterData.Id);
		}

		private Brush _Background;
		private Brush _Foreground;
		private double _FontSize;
		private FontFamily _FontFamily;
		public double FontSize
		{
			get
			{
				return _FontSize;
			}
			set
			{
				if (Math.Abs(_FontSize - value) >= 0.1)
				{
					_FontSize = value;
					NotifyPropertyChanged();
					AppGlobal.Settings.FontSize = value;
				}
			}
		}
		public FontFamily FontFamily
		{
			get
			{
				return _FontFamily;
			}
			set
			{
				if (value != null && !value.Source.Contains("Segoe"))
					_FontFamily = value;
				else
					_FontFamily = (FontFamily)App.Current.Resources["AppContentFontFamily"];
				NotifyPropertyChanged();
                AppGlobal.Settings.FontFamily = value;
			}
		}
		public Brush Foreground
		{
			get
			{
				if (AppGlobal.Settings.EnableAutomaticReadingTheme)
					return (SolidColorBrush)App.Current.Resources["AppForegroundBrush"];
				return _Foreground;
			}
			set
			{
				_Foreground = value;
				NotifyPropertyChanged();
                AppGlobal.Settings.Foreground = value;
			}
		}
		public Brush Background
		{
			get
			{
				if (AppGlobal.Settings.EnableAutomaticReadingTheme)
					return (SolidColorBrush)App.Current.Resources["AppBackgroundBrush"];
				return _Background;
			}
			set
			{
				_Background = value;
				NotifyPropertyChanged();
                AppGlobal.Settings.Background = value;
			}
		}

		IAsyncAction CurrentLoadingAction;

		public IAsyncAction LoadDataAsync(NovelPositionIdentifier nav)
		{
			if (CurrentLoadingAction != null)
			{
				CurrentLoadingAction.Cancel();
			}
			CurrentLoadingAction = AsyncInfo.Run(c => LoadDataInternalAsync(c, nav));
			return CurrentLoadingAction;
		}

		public async Task LoadDataInternalAsync(CancellationToken c, NovelPositionIdentifier nav)
		{
			if (nav.VolumeNo >= 0 && nav.ChapterNo >=0 && !String.IsNullOrEmpty(nav.SeriesId))
			{
				await LoadDataAsync(int.Parse(nav.SeriesId), nav.VolumeNo, nav.ChapterNo, nav.LineNo);
				return;
			}
			IsLoading = true;
			if (nav.ChapterNo < 0) nav.ChapterNo = 0;
			if (nav.VolumeNo < 0) nav.VolumeNo = 0;
			try
			{
				if (nav.ChapterId != null && nav.VolumeId == null && nav.SeriesId == null)
				{
					if (nav.SeriesId == null)
					{
						var chapter = await CachedClient.GetChapterAsync(nav.ChapterId);
						if (c.IsCancellationRequested)
						{
							IsLoading = false;
							return;
						}
						nav.SeriesId = chapter.ParentSeriesId;
						nav.VolumeId = chapter.ParentVolumeId;
					}
					var series = await CachedClient.GetSeriesAsync(nav.SeriesId);
					if (c.IsCancellationRequested)
					{
						IsLoading = false;
						return;
					} 
					var volume = series.Volumes.First(vol => vol.Id == nav.VolumeId);
					nav.VolumeNo = series.Volumes.IndexOf(volume);
					nav.ChapterNo = volume.Chapters.IndexOf(volume.Chapters.First(cpt => cpt.Id == nav.ChapterId));

				}
				else if (nav.VolumeId != null)
				{
					if (nav.SeriesId == null)
					{
						string volDesc = null;
						if (nav.SeriesId == null)
						{
							var volume = await CachedClient.GetVolumeAsync(nav.VolumeId);
							if (c.IsCancellationRequested)
							{
								IsLoading = false;
								return;
							} 
							nav.SeriesId = volume.ParentSeriesId;
							volDesc = volume.Description;
						}
					}
					var series = await CachedClient.GetSeriesAsync(nav.SeriesId);
					if (c.IsCancellationRequested)
					{
						IsLoading = false;
						return;
					}
					SeriesData = series;
					SeriesId = int.Parse(nav.SeriesId);
					VolumeData = SeriesData.Volumes.FirstOrDefault(vol => vol.Id == nav.VolumeId);
					if (VolumeData != null)
					{
						nav.VolumeNo = SeriesData.Volumes.IndexOf(VolumeData);
						VolumeNo = nav.VolumeNo;
					}
					else // This is the case that we need to refresh the series data!
					{
						series = await CachedClient.GetSeriesAsync(nav.SeriesId, true);
						if (c.IsCancellationRequested)
						{
							IsLoading = false;
							return;
						}
						SeriesData = series;
						VolumeData = SeriesData.Volumes.FirstOrDefault(vol => vol.Id == nav.VolumeId);
						nav.VolumeNo = SeriesData.Volumes.IndexOf(VolumeData);
						VolumeNo = nav.VolumeNo;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error in converting navigator data : {0}", ex.Message);

				if (c.IsCancellationRequested)
				{
					IsLoading = false;
					return;
				}

				Contents = new LineViewModel[] { 
					new LineViewModel(1,LoadingFailedContentOverallLabel),
					new LineViewModel(2,LoadingFailedContentNetworkIssue),
					new LineViewModel(3,"Navigator Resolve Exception : " + ex.Message) };
				IsLoading = false;
				LineNo = 0;
				return;
			}
			if (c.IsCancellationRequested)
			{
				IsLoading = false;
				return;
			} 
			await LoadDataInternalAsync(c,int.Parse(nav.SeriesId), nav.VolumeNo, nav.ChapterNo, nav.LineNo);

		}
		public IAsyncAction LoadDataAsync(int seriesId, int volumeNo = 0, int chapterNo = 0, int lineNo = 0, PreCachePolicy preCachePolicy = PreCachePolicy.CacheNext)
		{
			if (CurrentLoadingAction != null)
			{
				CurrentLoadingAction.Cancel();
			}
			CurrentLoadingAction = AsyncInfo.Run(c => LoadDataInternalAsync(c, seriesId, volumeNo, chapterNo, lineNo));
			return CurrentLoadingAction;
		}

		public async Task LoadDataInternalAsync(CancellationToken c, int seriesId, int volumeNo = 0, int chapterNo = 0, int lineNo = 0, PreCachePolicy preCachePolicy = PreCachePolicy.CacheNext)
		{
			IsLoading = true;
			try
			{
				if (seriesId > 0 && (SeriesData == null || seriesId != int.Parse(SeriesData.Id)))
				{
					if (DownloadingTask != null)
					{
						DownloadingTask.Cancel();
						try
						{
							await DownloadingTask;
						}
						catch (Exception)
						{
						}
						DownloadingTask = null;
						NotifyPropertyChanged("IsDownloading");
					}
					var series = await CachedClient.GetSeriesAsync(seriesId.ToString());
					if (c.IsCancellationRequested)
					{
						IsLoading = false;
						return;
					}
					SeriesData = series;
					SeriesId = seriesId;
				}
				if (volumeNo >= 0 && (VolumeData == null || VolumeData.Id != SeriesData.Volumes[volumeNo].Id))
				{
					VolumeData = SeriesData.Volumes[volumeNo];// = await CachedClient.GetVolumeAsync(SeriesData.Volumes[volumeNo.Value].Id);
					VolumeNo = volumeNo;
					var task = CachedClient.GetChapterAsync(VolumeData.Chapters[0].Id);
					//NotifyPropertyChanged("Index");
				}
				if (chapterNo >= 0 && (ChapterData == null || ChapterData.Id != VolumeData.Chapters[chapterNo].Id))
				{
					var chapter = await CachedClient.GetChapterAsync(VolumeData.Chapters[chapterNo].Id);
					if (c.IsCancellationRequested)
					{
						IsLoading = false;
						return;
					}
					// Fix for cached page leads to no content
					if (chapter.Lines.Count == 0)
					{
						chapter = await CachedClient.GetChapterAsync(VolumeData.Chapters[chapterNo].Id, true);
						if (c.IsCancellationRequested)
						{
							IsLoading = false;
							return;
						}
					}
					chapter.Title = VolumeData.Chapters[chapterNo].Title;
					ChapterData = chapter; //VolumeData.Chapters[chapterNo.Value] =
					ChapterNo = chapterNo;

					if (preCachePolicy == PreCachePolicy.CacheNext && HasNext)
					{
						// Pre caching next chapter
						CachedClient.GetChapterAsync(VolumeData.Chapters[chapterNo + 1].Id);
					}
					else if (preCachePolicy == PreCachePolicy.CachePrev && HasPrev)
					{
						CachedClient.GetChapterAsync(VolumeData.Chapters[chapterNo - 1].Id);
					}

					var lvms = _ChapterData.Lines.Select(line => new LineViewModel(line, ChapterData.Id));
					//_storage.AddRange(lvms);
					//NotifyOfInsertedItems(0, _storage.Count);
					//NotifyPropertyChanged("Contents");
					Contents = lvms.ToList();
					if (Contents.Count == 0)
					{
						Contents = new LineViewModel[] { 
							new LineViewModel(1,LoadingFailedContentOverallLabel),
							new LineViewModel(2,LoadingFailedContentSeviceIssue),
							new LineViewModel(3,"Detail : " + _ChapterData.ErrorMessage) };
					}
					//var collection = new PagelizedIncrementalVector<LineViewModel>(ChapterNo,new List<int>(VolumeData.Chapters.Select(c=>0)), lvms);
					//collection.AccuirePageData = async chptNo =>
					//{
					//	return (await CachedClient.GetChapterAsync(VolumeData.Chapters[chptNo].Id))
					//		.Lines.Select(line => new LineViewModel(line, ChapterData.Id)); 
					//};

					//collection.CollectionChanged += collection_CollectionChanged;

					//Contents = collection;
					//SeconderyIndex = VolumeData.Chapters;
					//NotifyPropertyChanged("SeconderyIndex");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				if (c.IsCancellationRequested)
				{
					IsLoading = false;
					return;
				} 
				Contents = new LineViewModel[] { 
					new LineViewModel(1,LoadingFailedContentOverallLabel),
					new LineViewModel(2,LoadingFailedContentNetworkIssue),
					new LineViewModel(3,"Detail : " + ex.Message) };
				IsLoading = false;
				LineNo = 0;
				return;
			}

			IsLoading = false;


			if (lineNo >= Contents.Count)
				lineNo = 0;
			if (lineNo >= 0)
				LineNo = lineNo;
			else
				LineNo = Contents.Count + lineNo;

			var bookmark = CreateBookmark();

			await App.Current.MainDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
				 await AppGlobal.UpdateHistoryListAsync(bookmark);
				 await AppGlobal.UpdateSecondaryTileAsync(bookmark);
			});

			if (c.IsCancellationRequested)
			{
				IsLoading = false;
				return;
			}

			if (EnableComments && Contents != null)
			{
				try
				{
					var CommentsList = await LightKindomHtmlClient.GetCommentedLinesListAsync(ChapterData.Id);
					foreach (var cln in CommentsList)
					{
						((LineViewModel)Contents[cln - 1]).MarkAsCommented();
					}
					if (CommentsListLoaded != null)
						CommentsListLoaded.Invoke(this, CommentsList);
					//if (!IsFavored && App.User != null && App.User.FavoriteList != null && App.User.FavoriteList.Any(fav=>fav.SeriesTitle == SeriesData.Title))
					//{
					//	Debug.WriteLine("Adding Current Volume to User Favorite");
					//	await AddCurrentVolumeToFavoriteAsync();
					//}
				}
				catch (Exception exception)
				{
					Debug.WriteLine(exception);
				}

			}
		}

		public async Task LoadCommentsListAsync()
		{
			if (EnableComments && Contents != null)
			{
				try
				{
					var CommentsList = await LightKindomHtmlClient.GetCommentedLinesListAsync(ChapterData.Id);
					foreach (var cln in CommentsList)
					{
						((LineViewModel)Contents[cln - 1]).MarkAsCommented();
					}
					if (CommentsListLoaded != null)
						CommentsListLoaded.Invoke(this, CommentsList);
					//if (!IsFavored && App.User != null && App.User.FavoriteList != null && App.User.FavoriteList.Any(fav=>fav.SeriesTitle == SeriesData.Title))
					//{
					//	Debug.WriteLine("Adding Current Volume to User Favorite");
					//	await AddCurrentVolumeToFavoriteAsync();
					//}
				}
				catch (Exception exception)
				{
					Debug.WriteLine(exception);
				}

			}
		}

		public async Task<bool> RefreshSeriesDataAsync()
		{
			IsLoading = true;
			try
			{
				SeriesData = await CachedClient.GetSeriesAsync(SeriesId.ToString(), true);
				VolumeData = SeriesData.Volumes[VolumeNo];
				IsLoading = false;
				return true;
			}
			catch (Exception)
			{
				IsLoading = false;
				return false;
			}
		}

		public bool IsDownloading { get { return DownloadingTask != null; } }
		public double DownloadingProgress { get; set; }

		IAsyncActionWithProgress<string> DownloadingTask = null;

		public async Task CancelCachingRequestAsync()
		{
			if (DownloadingTask != null)
			{
				DownloadingTask.Cancel();
				try
				{
					await DownloadingTask;
				}
				catch (Exception)
				{
				}
				DownloadingTask = null;
				NotifyPropertyChanged("IsDownloading");
			}
		}

		public async Task<bool?> CachingRestChaptersAsync(bool cacheImages = true)
		{

			if (DownloadingTask != null)
			{
				DownloadingTask.Cancel();
				try
				{
					await DownloadingTask;
				}
				catch (Exception)
				{
				}
				return true;
			}
			else if (IsLoading)
				return false;

			//IsLoading = true; 
			var chapters = new List<string>();
			for (int i = ChapterNo + 2; i < VolumeData.Chapters.Count; i++)
			{
				chapters.Add(VolumeData.Chapters[i].Id);
			}
			for (int i = VolumeNo + 1; i < SeriesData.Volumes.Count; i++)
			{
				chapters.AddRange(SeriesData.Volumes[i].Chapters.Select(c => c.Id));
			}

			DownloadingTask = CachedClient.CacheChaptersAsync(chapters, cacheImages);

			DownloadingProgress = 0;
			NotifyPropertyChanged("IsDownloading");
			NotifyPropertyChanged("DownloadingProgress");

			DownloadingTask.Progress = (asyncInfo, progressInfo) =>
			{
				foreach (var vvm in Index)
				{
					var cvm = vvm.FirstOrDefault(c => c.Id == progressInfo);
					if (cvm != null)
					{
						cvm.NotifyPropertyChanged("IsDownloaded");
						break;
					}
				}
				DownloadingProgress += 1.0 / chapters.Count;
				NotifyPropertyChanged("DownloadingProgress");
			};
			bool result = false;
			try
			{
				await DownloadingTask;
				if (DownloadingTask.AsTask().IsCanceled)
					return null;
				result = DownloadingProgress > 0.99;
			}
			catch (Exception)
			{

			}
			DownloadingTask = null;
			NotifyPropertyChanged("IsDownloading");
			NotifyPropertyChanged("IsDownloaded");
			return result;
			//IsLoading = false;
		}

		void collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Debug.WriteLine(String.Format("Contents Collection size changed, Count = {0}", Contents.Count));
		}

		public bool IsDataLoaded
		{
			get
			{
				return ChapterData != null && VolumeData != null && SeriesData != null;
			}
		}

		internal void SetCustomizeCover(Uri uri)
		{
		}

		public BookmarkInfo CreateBookmark()
		{
			var bookmark = new BookmarkInfo
			{
				ViewDate = DateTime.Now,
				Position = new NovelPositionIdentifier
				{
					ChapterNo = ChapterNo,
					VolumeNo = VolumeNo,
					SeriesId = SeriesId.ToString(),//App.CurrentSeries.Id,
					LineNo = LineNo,
				},
				Progress = Contents.Count != 0 ? (LineNo / Contents.Count) : 0,
			};

			if (IsDataLoaded)
			{
				bookmark.ChapterTitle = ChapterData.Title;
				bookmark.VolumeTitle = VolumeData.Title;
				bookmark.SeriesTitle = SeriesData.Title;
				bookmark.DescriptionThumbnailUri = VolumeData.CoverImageUri;
				bookmark.DescriptionImageUri = VolumeData.CoverImageUri;

				if (CachedClient.ChapterCache.ContainsKey(VolumeData.Chapters[0].Id) && CachedClient.ChapterCache[VolumeData.Chapters[0].Id].IsCompleted && !CachedClient.ChapterCache[VolumeData.Chapters[0].Id].IsFaulted)
				{
					// Find the First Illustration of current Volume
					var imageLine = CachedClient.ChapterCache[VolumeData.Chapters[0].Id].Result.Lines.FirstOrDefault(line => line.ContentType == LineContentType.ImageContent);
					if (imageLine != null)
						bookmark.DescriptionImageUri = imageLine.Content;
				}
				else
				{
					var imageLine = Contents.Cast<LineViewModel>().FirstOrDefault(line => line.IsImage);
					if (imageLine != null)
						bookmark.DescriptionImageUri = imageLine.Content;
				}

				var textLines = from line in Contents.Cast<LineViewModel>()
								where !line.IsImage && line.No >= LineNo && line.No <= LineNo + 5
								select line.Content;
				var builder = new StringBuilder();
				bookmark.ContentDescription = textLines.Aggregate(builder, (b, s) => { b.AppendLine(s); return b; }).ToString();
			}
			else
			{
				bookmark.ContentDescription = "Failed to load data, please try again when you connected";
			}
			return bookmark;
		}

		// When LineNo/PageNo Changed by UI Report, this flag will be TRUE, When User set, this will be FALSE
		public bool SuppressViewChange { get; set; }
		// Use this method to set the LineNo and PageNo without calling NotifyPropertyChanged Event
		public void ReportViewChanged(int? pageNo, int? lineNo = null)
		{
			SuppressViewChange = true;
			if (lineNo != null)
			{
				_LineNo = lineNo.Value;
				NotifyPropertyChanged("LineNo");
			}
			if (pageNo != null)
			{
				_PageNo = pageNo.Value;
				NotifyPropertyChanged("PageNo");
			}
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
	public class BookCoverViewModel : INotifyPropertyChanged
	{
		public BookCoverViewModel()
		{ }

		public BookCoverViewModel(BookItem item)
		{
			Title = item.Title;
			Subtitle = item.Subtitle;
			Id = item.Id;
			CoverImageUri = item.CoverImageUri;
			ItemType = item.ItemType;
			SubtileLabel = item.VolumeNo;
		}

		string _title;
		public string Title
		{
			get { return _title; }
			set
			{
				_title = value;
				NotifyPropertyChanged();
			}
		}
		string _subtitle;
		public string Subtitle
		{
			get { return _subtitle; }
			set
			{
				_subtitle = value;
				NotifyPropertyChanged();
			}
		}
		string _subtileLabel;
		public string SubtileLabel
		{
			get { return _subtileLabel; }
			set
			{
				_subtileLabel = value;
				NotifyPropertyChanged();
			}
		}

		public async Task LoadDescriptionAsync()
		{
			if (ItemType == BookItemType.Volume)
			{
				var vol = await CachedClient.GetVolumeAsync(Id);
				Description = vol.Description;
			}
			else if (ItemType == BookItemType.Series)
			{
				var ser = await CachedClient.GetSeriesAsync(Id);
				Description = ser.Description;
			}

		}

		string _description;
		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
				NotifyPropertyChanged();
			}
		}

		string _id;
		public string Id
		{
			get { return _id; }
			set
			{
				_id = value;
				NotifyPropertyChanged();
			}
		}

		public string NavigateUri
		{
			get
			{
				switch (ItemType)
				{
					case BookItemType.Series:
						return "/SeriesViewPage.xaml?id=" + Id;
					case BookItemType.Volume:
						return String.Format("/SeriesViewPage.xaml?id={0}&volume={1}", "", Id);
					case BookItemType.Chapter:
						return "/ChapterViewPage.xaml?id=" + Id;
				}

				return "/";
			}
		}

		public BookItemType ItemType { get; set; }

		public string CoverImageUri
		{
			get { return _coverUri; }
			set
			{
				_coverUri = value;
				NotifyPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private string _coverUri;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public class SeriesPreviewModel : INotifyPropertyChanged
	{
		string _title;
		public string Title
		{
			get { return _title; }
			set
			{
				_title = value;
				NotifyPropertyChanged();
			}
		}
		string _id;
		public string ID
		{
			get { return _id; }
			set
			{
				_id = value;
				NotifyPropertyChanged();
			}
		}

		public string NavigateUri
		{
			get { return "/SeriesViewPage.xaml?id=" + ID; }
		}

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}


	public class HistoryItemViewModel : INotifyPropertyChanged
	{
		public HistoryItemViewModel() { }
		public HistoryItemViewModel(BookmarkInfo item)
		{
			Position = item.Position;
			ProgressPercentage = item.Progress;
			CoverImageUri = item.DescriptionImageUri;
			Description = item.ContentDescription;
			ChapterTitle = item.ChapterTitle;
			VolumeTitle = item.VolumeTitle;
			SeriesTitle = item.SeriesTitle;
			UpdateTime = item.ViewDate;
		}
		public NovelPositionIdentifier Position { get; set; }

		public double ProgressPercentage { get; set; }
		public string NavigateUri
		{
			get { return String.Format("/ChapterViewPage.xaml?id={0}&line={1}", Position.ChapterId, Position.LineNo); }
		}
		public DateTime UpdateTime { get; set; }

		private string _coverImageUri;
		public string CoverImageUri
		{
			get
			{
				return _coverImageUri;
			}
			set
			{
				if (_coverImageUri != value)
				{
					_coverImageUri = value;
					NotifyPropertyChanged();
				}
			}
		}
		private string _description;
		public string Description
		{
			get
			{
				return _description;
			}
			set
			{
				if (_description != value)
				{
					_description = value;
					NotifyPropertyChanged();
				}
			}
		}
		public string SeriesTitle { get; set; }
		public string VolumeTitle { get; set; }
		public string ChapterTitle { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	public class SeriesViewModel : KeyGroup<string, VolumeViewModel>, INotifyPropertyChanged
	{
		private string _title;
		private string _author;
		private string _illustrator;
		private string _publisher;
		private DateTime _updateTime;
		private string _description;
		private Uri _coverImageUri;
		public string Title
		{
			get { return _title; }
			set
			{
				_title = value;
				NotifyPropertyChanged();
			}
		}
		public string Author
		{
			get { return _author; }
			set
			{
				_author = value;
				NotifyPropertyChanged();
			}
		}
		public string Illustrator
		{
			get { return _illustrator; }
			set
			{
				_illustrator = value;
				NotifyPropertyChanged();
			}
		}
		public string Publisher
		{
			get { return _publisher; }
			set
			{
				_publisher = value;
				NotifyPropertyChanged();
			}
		}
		public DateTime UpdateTime
		{
			get { return _updateTime; }
			set
			{
				_updateTime = value;
				NotifyPropertyChanged();
			}
		}
		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
				NotifyPropertyChanged();
			}
		}
		public Uri CoverImageUri
		{
			get { return _coverImageUri; }
			set
			{
				if (_coverImageUri != value)
				{
					_coverImageUri = value;
					NotifyPropertyChanged();
				}
			}
		}

		public string Id
		{
			get;
			set;
		}
		public SeriesViewModel()
		{
			_isLoading = false;
		}

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

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	public class VolumeViewModel : IList<ChapterPreviewModel>, INotifyPropertyChanged
	{

		#region Self Properties
		private string _title;
		private string _author;
		private string _illustrator;
		private string _publisher;
		private DateTime _updateTime;
		private string _description;
		private Uri _coverImageUri;
		public string Title
		{
			get { return _title; }
			set
			{
				_title = value;
				NotifyPropertyChanged();
			}
		}
		public string Author
		{
			get { return _author; }
			set
			{
				_author = value;
				NotifyPropertyChanged();
			}
		}
		public string Illustrator
		{
			get { return _illustrator; }
			set
			{
				_illustrator = value;
				NotifyPropertyChanged();
			}
		}
		public string Publisher
		{
			get { return _publisher; }
			set
			{
				_publisher = value;
				NotifyPropertyChanged();
			}
		}
		public DateTime UpdateTime
		{
			get { return _updateTime; }
			set
			{
				_updateTime = value;
				NotifyPropertyChanged();
			}
		}
		public string Description
		{
			get { return _description; }
			set
			{
				//if (value == "" || value == null)
				//	_description = AppResources.NoDescription;
				_description = value;
				NotifyPropertyChanged();
			}
		}
		public Uri CoverImageUri
		{
			get { return _coverImageUri; }
			set
			{
				if (_coverImageUri != value)
				{
					_coverImageUri = value;
					NotifyPropertyChanged();
				}
			}
		}

		public Uri FirstIllustrationUri
		{
			get;
			set;
		}

		public IList<ChapterPreviewModel> Chapters { get; set; }

		private bool _isLoading;
		public bool IsLoading
		{
			get { return _isLoading; }
			set
			{
				if (_isLoading == value) return;
				_isLoading = value;
				NotifyPropertyChanged();
			}
		}
		#endregion


		#region IList

		public void Add(ChapterPreviewModel value)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(ChapterPreviewModel value)
		{
			Debug.WriteLine("Contains Get Called");
			return Chapters.Contains(value);
		}

		public int IndexOf(ChapterPreviewModel value)
		{
			Debug.WriteLine("IndexOf Get Called");
			return Chapters.IndexOf(value);
		}

		public void Insert(int index, ChapterPreviewModel value)
		{
			throw new NotImplementedException();
		}

		public bool IsFixedSize
		{
			get { return true; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public bool Remove(ChapterPreviewModel value)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public ChapterPreviewModel this[int index]
		{
			get
			{
				if (index >= Chapters.Count)
					Debug.WriteLine("Shit! Index out of range at : " + index);
				//Debug.WriteLine(String.Format("this[{0}]",index));
				return Chapters[index];
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void CopyTo(ChapterPreviewModel[] array, int index)
		{
			//Debug.WriteLine("CopyTo Called");
			((IList)Chapters).CopyTo(array, index);
		}

		public int Count
		{
			get
			{
				Debug.WriteLine("Count Get Called");
				return Chapters.Count;
			}
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			Debug.WriteLine("GetEnumerator() Get Called");
			return Chapters.GetEnumerator();
		}
		public IEnumerator<ChapterPreviewModel> GetEnumerator()
		{
			Debug.WriteLine("GetEnumerator() Get Called");
			return Chapters.GetEnumerator();
		}
		#endregion

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public string Id { get; set; }

		public string Label { get; set; }
	}


	public class ChapterPreviewModel : INotifyPropertyChanged
	{
		string _title;
		public string Title
		{
			get { return _title; }
			set
			{
				_title = value;
				NotifyPropertyChanged();
			}
		}
		string _id;
		public string Id
		{
			get { return _id; }
			set
			{
				_id = value;
				NotifyPropertyChanged();
			}
		}
		int _no;
		public int No
		{
			get { return _no; }
			set
			{
				_no = value;
				NotifyPropertyChanged();
			}
		}

		public int VolumeNo
		{
			get;
			set;
		}

		public bool IsDownloaded { get { return Common.CachedClient.IsChapterCached(_id); } }
		public string NavigateUri
		{
			get { return "/ChapterViewPage.xaml?id=" + Id; }
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

	public class Comment : INotifyPropertyChanged
	{
		private string _author;
		private DateTime _date;
		private string _content;

		public Comment()
		{
			_content = "uninitialized";
			_author = "unknown";
		}

		public Comment(string content)
		{
			_content = content;
			_date = DateTime.MinValue;
			_author = null;
		}

		public string Content
		{
			get { return _content; }
			set
			{
				_content = value;
				NotifyPropertyChanged();
			}
		}

		public DateTime Date
		{
			get { return _date; }
			set
			{
				_date = value;
				NotifyPropertyChanged();
			}
		}

		public string Author
		{
			get { return _author; }
			set
			{
				_author = value;
				NotifyPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}

	public class LineViewModel : INotifyPropertyChanged
	{
		private Task<IEnumerable<string>> LoadCommentTask;
		private string _content;
		private bool _isLoading;
		private ObservableCollection<Comment> _comments;

		public string ParentChapterId { get; set; }

		public LineContentType ContentType
		{
			get
			{
				if (IsImage)
					return LineContentType.ImageContent;
				return LineContentType.TextContent;
			}
		}

		public int No { get; set; }

		public LineViewModel()
		{
			_content = null;
		}

		public LineViewModel(Line line, string chapterId)
		{
			//if (line.ContentType == LineContentType.TextContent)
			//	_content = line.Content; // Add the indent
			//else

			IsImage = line.ContentType == LineContentType.ImageContent;
#if WINDOWS_PHONE_APP
			if (!IsImage)
				_content = "　" + line.Content; // Indent
			else
				_content = line.Content;
#else
			_content = line.Content;
#endif

			ParentChapterId = chapterId;
			No = line.No;
		}

		public bool IsImageCached
		{
			get { return _content.StartsWith("http://") && CachedClient.IsIllustrationCached(_content); }
		}

		public Uri ImageUri
		{
			get
			{
				if (!_content.StartsWith("http://"))
					return null;
				return CachedClient.GetIllustrationCachedUri(_content);
			}
		}

		public LineViewModel(string content)
		{
			_content = content;
		}
		public LineViewModel(int id, string content)
		{
			No = id;
			_content = content;
			IsImage = false;
		}
		public LineViewModel(string content, params string[] comments)
		{
			_content = content;
			Comments = new ObservableCollection<Comment>(comments.Select(comment => new Comment(comment)));
			Comments.CollectionChanged += Comments_CollectionChanged;
		}

		public void MarkAsCommented()
		{
			if (Comments != null)
				return;
			Comments = new ObservableCollection<Comment>();
			Comments.CollectionChanged += Comments_CollectionChanged;
			NotifyPropertyChanged("HasNoComment");
			NotifyPropertyChanged("HasComment");
		}

		public async Task<bool> AddCommentAsync(string commentText)
		{
			if (!string.IsNullOrEmpty(commentText) && commentText.Length < 300 && !String.IsNullOrEmpty(ParentChapterId) && AppGlobal.IsSignedIn)
			{
				if (HasNoComment)
					MarkAsCommented();
				Comments.Add(new Comment(commentText));
				try
				{
					var result = await LightKindomHtmlClient.CreateCommentAsync(No.ToString(), ParentChapterId, commentText);
					if (!result)
					{
						// Re-signin
						result = await AppGlobal.SignInAutomaticllyAsync(true);
						if (!result)
							return false;
						// And retry
						result = await LightKindomHtmlClient.CreateCommentAsync(No.ToString(), ParentChapterId, commentText);
					}
					return result;
				}
				catch
				{
					return false;
				}
			}
			return false;
		}

		void Comments_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			NotifyPropertyChanged("CommentsNotice");
		}

		public virtual string Content
		{
			get { return _content; }
			set
			{
				_content = value;
				NotifyPropertyChanged("Content");
			}
		}

		public string CommentsNotice
		{
			get
			{
				if (Comments != null)
					return String.Format("{0} comments", Comments.Count);
				else
					return null;
			}
		}

		public bool IsLoading
		{
			get { return _isLoading; }
			set
			{
				if (_isLoading == value) return;
				_isLoading = value;
				NotifyPropertyChanged("IsLoading");
			}
		}

		public bool HasComments
		{
			get { return Comments != null; }
		}

		public bool HasNoComment
		{
			get { return Comments == null; }
		}

		public bool IsImage
		{
			get;
			set;
			//get { return Uri.IsWellFormedUriString(_content, UriKind.Absolute); }
		}

		public ObservableCollection<Comment> Comments
		{
			get { return _comments; }
			set
			{
				if (value != _comments)
				{
					_comments = value;
					NotifyPropertyChanged("Comments");
					NotifyPropertyChanged("HasComments");
					NotifyPropertyChanged("HasNoComments");
				}
			}
		}

		public async Task LoadCommentsAsync()
		{
			if (HasNoComment || String.IsNullOrEmpty(ParentChapterId) || IsLoading)
				return;
			if (LoadCommentTask == null && Comments.Count == 0)
			{
				string lineId = No.ToString();
				Debug.WriteLine("Loading Comments : line_id = " + lineId + " ,chapter_id = " + ParentChapterId);
				LoadCommentTask = LightKindomHtmlClient.GetCommentsAsync(lineId, ParentChapterId);
				IsLoading = true;
				try
				{
					var comments = await LoadCommentTask;
					foreach (var comment in comments)
					{
						Comments.Add(new Comment(comment));
					}
					LoadCommentTask = null;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Comments load failed : line_id = " + lineId + " ,chapter_id = " + ParentChapterId + "exception : " + ex.Message);
				}
				IsLoading = false;
			}
			return;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

	}

}