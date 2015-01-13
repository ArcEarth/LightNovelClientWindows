using LightNovel.Common;
using LightNovel.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
	public class ReadingPageViewModel : INotifyPropertyChanged
	{
		public ReadingPageViewModel()
		{

			//_FontSize = (double)App.Current.Resources["AppFontSizeMediumLarge"];
			//_Background = (SolidColorBrush)App.Current.Resources["AppReadingBackgroundBrush"];
			//_Foreground = (SolidColorBrush)App.Current.Resources["AppForegroundBrush"];
			_FontSize = App.Current.Settings.FontSize;
			_Background = App.Current.Settings.Background;
			_Foreground = App.Current.Settings.Foreground;
			//this.PropertyChanged += ReadingPageViewModel_PropertyChanged;
		}

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
						ChapterData = VolumeData.Chapters[ChapterNo] = await CachedClient.GetChapterAsync(VolumeData.Chapters[ChapterNo].Id);
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
					Index = _SeriesData.Volumes;
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
					SeconderyIndex = _VolumeData.Chapters;
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
				}
			}
		}
		public String Header
		{
			get
			{
				if (SeriesData != null && VolumeData != null && ChapterData != null)
					return SeriesData.Title + " / " + VolumeData.Title + " / " + ChapterData.Title;
				else
					return "Loading...";
			}
		}

		private bool _IsLoading = false;
		private int _SeriesId = -1;
		private int _VolumeNo = -1;
		private int _ChapterNo = -1;
		private int _LineNo = -1;
		private int _PageNo = -1;
		private int _PagesCount = -1;
		private IList<LineViewModel> _Contents;
		private IList<Volume> _Index;
		private IList<Chapter> _SeconderyIndex;

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
		public int SeriesId
		{
			get { return _SeriesId; }
			private set
			{
				if (_SeriesId != value)
				{
					_SeriesId = value;
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
					NotifyPropertyChanged();
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
					NotifyPropertyChanged();
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

		public bool EnableComments
		{
			get
			{
				return App.Current.Settings.EnableComments;
			}
			set
			{
				App.Current.Settings.EnableComments = value;
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

		public IList<LineViewModel> Contents
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

		public IList<Volume> Index
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

		public IList<Chapter> SeconderyIndex
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

		public bool IsFavored
		{
			get
			{
				if (Index == null || App.Current.User == null || App.Current.User.FavoriteList == null)
					return false;
				return App.Current.User.FavoriteList.Any(vol => vol.VolumeId == VolumeData.Id);
			}
		}

		public async Task<bool> AddCurrentVolumeToFavoriteAsync()
		{
			var result = await App.Current.User.AddUserFavriteAsync(VolumeData,SeriesData.Title);
			if (result)
				NotifyPropertyChanged("IsFavored");
			return result;
		}

		public async Task<bool> RemoveCurrentVolumeFromFavoriteAsync()
		{
			var favol = App.Current.User.FavoriteList.FirstOrDefault(fav => fav.VolumeId == VolumeData.Id);
			if (favol == null)
				return true;
			var result = await App.Current.User.RemoveUserFavriteAsync(favol.FavId);
			if (result)
				NotifyPropertyChanged("IsFavored");
			return result;
		}

		public Task<IEnumerable<string>> GetComentsAsync(int LineNo)
		{
			return LightKindomHtmlClient.GetCommentsAsync(LineNo.ToString(), _ChapterData.Id);
		}

		private Brush _Background;
		private Brush _Foreground;
		private double _FontSize;
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
					App.Current.Settings.FontSize = value;
				}
			}
		}
		public Brush Foreground
		{
			get
			{
				return _Foreground;
			}
			set
			{
				_Foreground = value;
				NotifyPropertyChanged();
				App.Current.Settings.Foreground = value;
			}
		}
		public Brush Background
		{
			get
			{
				return _Background;
			}
			set
			{
				_Background = value;
				NotifyPropertyChanged();
				App.Current.Settings.Background = value;
			}
		}

		public async Task LoadDataAsync(NovelPositionIdentifier nav)
		{
			if (IsLoading)
				return;
			IsLoading = true;
			if (nav.ChapterNo < 0) nav.ChapterNo = 0;
			if (nav.VolumeNo < 0) nav.VolumeNo = 0;
			if (nav.ChapterId != null && nav.VolumeId == null && nav.SeriesId == null)
			{
				try
				{
					if (nav.SeriesId == null)
					{
						var chapter = await CachedClient.GetChapterAsync(nav.ChapterId);
						nav.SeriesId = chapter.ParentSeriesId;
						nav.VolumeId = chapter.ParentVolumeId;
					}
					var series = await CachedClient.GetSeriesAsync(nav.SeriesId);
					var volume = series.Volumes.First(vol => vol.Id == nav.VolumeId);
					nav.VolumeNo = series.Volumes.IndexOf(volume);
					nav.ChapterNo = volume.Chapters.IndexOf(volume.Chapters.First(cpt => cpt.Id == nav.ChapterId));
				}
				catch (Exception exception)
				{
					Debug.WriteLine("Error in converting navigator data : {0}", exception.Message);
					return;
				}
			}
			else if (nav.VolumeId != null && nav.SeriesId == null)
			{
				try
				{
					if (nav.SeriesId == null)
					{
						var volume = await CachedClient.GetVolumeAsync(nav.VolumeId);
						nav.SeriesId = volume.ParentSeriesId;
					}
					var series = await CachedClient.GetSeriesAsync(nav.SeriesId);
					var result = series.Volumes.FirstOrDefault(vol => vol.Id == nav.VolumeId);
					if (result != null)
						nav.VolumeNo = series.Volumes.IndexOf(result);
					else // This is the case that we need to refresh the series data!
					{
						series = await CachedClient.GetSeriesAsync(nav.SeriesId, true);
						result = series.Volumes.FirstOrDefault(vol => vol.Id == nav.VolumeId);
						nav.VolumeNo = series.Volumes.IndexOf(result);
					}
				}
				catch (Exception exception)
				{
					Debug.WriteLine("Error in converting navigator data : {0}", exception.Message);
					return;
				}
			}
			await LoadDataAsync(int.Parse(nav.SeriesId), nav.VolumeNo, nav.ChapterNo, nav.LineNo);

		}

		public async Task LoadDataAsync(int? seriesId, int? volumeNo = 0, int? chapterNo = 0, int? lineNo = null)
		{
			IsLoading = true;
			try
			{
				if (seriesId != null && (SeriesData == null || seriesId != int.Parse(SeriesData.Id)))
				{
					SeriesData = await CachedClient.GetSeriesAsync(seriesId.ToString());
					SeriesId = seriesId.Value;
				}
				if (volumeNo != null && (VolumeData == null || VolumeData.Id != SeriesData.Volumes[volumeNo.Value].Id))
				{
					VolumeData = SeriesData.Volumes[volumeNo.Value];// = await CachedClient.GetVolumeAsync(SeriesData.Volumes[volumeNo.Value].Id);
					VolumeNo = volumeNo.Value;
					var task = CachedClient.GetChapterAsync(VolumeData.Chapters[0].Id);
					//NotifyPropertyChanged("Index");
				}
				if (chapterNo != null && (ChapterData == null || ChapterData.Id != VolumeData.Chapters[chapterNo.Value].Id))
				{
					var chapter = await CachedClient.GetChapterAsync(VolumeData.Chapters[chapterNo.Value].Id);
					chapter.Title = VolumeData.Chapters[chapterNo.Value].Title;
					ChapterData = chapter; //VolumeData.Chapters[chapterNo.Value] =
					ChapterNo = chapterNo.Value;
					Contents = _ChapterData.Lines.Select(line => new LineViewModel(line,ChapterData.Id)).ToList();

					//SeconderyIndex = VolumeData.Chapters;
					//NotifyPropertyChanged("SeconderyIndex");
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				Contents = new LineViewModel[] { new LineViewModel(1,"Failed to load data :("),
										new LineViewModel(2,"Please check your Internet Connectivity."),
										new LineViewModel(3,"Exception detail : " + ex.Message) };
				IsLoading = false;
				if (lineNo != null) //&& LineNo != lineNo.Value)
				{
					LineNo = lineNo.Value;
				} 
				return;
			}

			IsLoading = false;

			if (lineNo != null) //&& LineNo != lineNo.Value)
			{
				LineNo = lineNo.Value;
			}

			if (EnableComments && Contents!=null)
			{
				var CommentsList = await LightKindomHtmlClient.GetCommentedLinesListAsync(ChapterData.Id);
				foreach (var cln in CommentsList)
				{
					Contents[cln - 1].MarkAsCommented();
				}
				if (CommentsListLoaded != null)
					CommentsListLoaded.Invoke(this, CommentsList);
			}
		}

		public bool IsDataLoaded
		{
			get
			{
				return ChapterData != null && VolumeData != null && SeriesData != null;
			}
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
				Progress = LineNo / Contents.Count,
			};

			if (IsDataLoaded)
			{
				bookmark.ChapterTitle = ChapterData.Title;
				bookmark.VolumeTitle = VolumeData.Title;
				bookmark.SeriesTitle = SeriesData.Title;

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
					var imageLine = Contents.FirstOrDefault(line => line.ContentType == LineContentType.ImageContent);
					if (imageLine != null)
						bookmark.DescriptionImageUri = imageLine.Content;
				}

				var textLines = from line in Contents
								where line.ContentType == LineContentType.TextContent && line.No >= LineNo && line.No <= LineNo + 5
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
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
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
	public class SeriesViewModel : INotifyPropertyChanged
	{
		private Series _dataConrtext;

		public Series DataContext
		{
			get { return _dataConrtext; }
			set
			{
				if (_dataConrtext != value)
				{
					LoadData(value);
					_dataConrtext = value;
					NotifyPropertyChanged();
				}
			}
		}
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
			Volumes = new ObservableCollection<VolumeViewModel>();
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
		public void Load(Series series)
		{
			DataContext = series;
		}
		public async Task LoadDataAsync(string serId, string volId = "", string cptId = "")
		{
			IsLoading = true;
			Id = serId;
			try
			{
				var series = await CachedClient.GetSeriesAsync(serId);
				DataContext = series;
				//foreach (var vvm in Volumes)
				//{
				//	await vvm.LoadDataAsync(vvm.Id);
				//}
			}
			catch (Exception exception)
			{
				IsLoading = false;
				Id = null;
				throw exception;
				//MessageBox.Show("Network issue occured, please check your wi-fi or data setting and try again.\nError Code:" + exception.Message, "Network issue", MessageBoxButton.OK);
			}
		}

		private void LoadData(Series series)
		{
			IsLoading = true;

			Title = series.Title;
			Author = series.Author;
			CoverImageUri = new Uri(series.CoverImageUri);
			Illustrator = series.Illustrator;
			Volumes.Clear();
			foreach (var vol in series.Volumes)
			{
				var vvm = new VolumeViewModel
				{
					DataContext = vol
				};
				Volumes.Add(vvm);
			}
			IsLoading = false;
		}
		public ObservableCollection<VolumeViewModel> Volumes { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	public class VolumeViewModel : INotifyPropertyChanged
	{
		private Volume _dataConrtext;

		public Volume DataContext
		{
			get { return _dataConrtext; }
			set
			{
				if (_dataConrtext != value)
				{
					LoadData(value);
					_dataConrtext = value;
					NotifyPropertyChanged();
				}
			}
		}
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

		public VolumeViewModel()
		{
			_isLoading = false;
			ChapterList = new ObservableCollection<ChapterPreviewModel>();
		}
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
		public async Task LoadDataAsync(string id)
		{
			//if (Id == id) return;
			IsLoading = true;
			Id = id;
			try
			{
				var volume = await CachedClient.GetVolumeAsync(id);
				DataContext = volume;
				IsLoading = true;
				var firstChapter = await CachedClient.GetChapterAsync(volume.Chapters[0].Id);
				var imageLine = firstChapter.Lines.FirstOrDefault(line => line.ContentType == LineContentType.ImageContent);
				if (imageLine != null)
					FirstIllustrationUri = new Uri(imageLine.Content);
				IsLoading = false;
				//Title = volume.Title;
				//Author = volume.Author;
				//Description = volume.Description;
				//CoverImageUri = new Uri(volume.CoverImageUri);
				//Illustrator = volume.Illustrator;
				//ChapterList.Clear();
				//int no = 0;
				//foreach (var cp in volume.ChapterDescriptorList)
				//{
				//    var cpvm = new ChapterPreviewModel
				//    {
				//        Title = cp.Title,
				//        Id = cp.Id,
				//        No = no++
				//    };
				//    ChapterList.Add(cpvm);
				//}
				//IsLoading = false;
			}
			catch (Exception exception)
			{
				IsLoading = false;
				Id = null;
				throw exception;
				//MessageBox.Show("Network issue occured, please check your wi-fi or data setting and try again.\nError Code:" + exception.Message, "Network issue", MessageBoxButton.OK);
			}
		}

		private void LoadData(Volume volume)
		{
			IsLoading = true;
			if (Id == volume.Id)
			{
				Description = volume.Description;
				return;
			}

			Id = volume.Id;
			Title = volume.Title;
			Author = volume.Author;
			Illustrator = volume.Illustrator;
			Publisher = volume.Publisher;
			Description = volume.Description;
			CoverImageUri = new Uri(volume.CoverImageUri);
			ChapterList.Clear();
			foreach (var cp in volume.Chapters)
			{
				var cpvm = new ChapterPreviewModel
				{
					Id = cp.Id,
					No = cp.ChapterNo,
					Title = cp.Title,
				};
				ChapterList.Add(cpvm);
			}
			IsLoading = false;
		}
		public ObservableCollection<ChapterPreviewModel> ChapterList { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public string Id { get; set; }
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

		public bool IsDownloaded { get; set; }
		public string NavigateUri
		{
			get { return "/ChapterViewPage.xaml?id=" + Id; }
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
	public class ContentSectionViewModel : INotifyPropertyChanged
	{
		private Chapter _dataContext;
		public Chapter DataContext
		{
			get { return _dataContext; }
			private set
			{
				if (_dataContext == value) return;
				LoadData(value);
				_dataContext = value;
				NotifyPropertyChanged();
				CurrentLineNo = 1;
			}
		}
		//string NovelUrl;
		//string NovelText;
		public ObservableCollection<LineViewModel> Lines
		{
			get { return _lines; }
			set
			{
				_lines = value;
				NotifyPropertyChanged();
			}
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

		public int ChapterNo
		{
			get;
			set;
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

		public double ProgressPercentage
		{
			get
			{
				if (Lines.Count > 0)
					return (double)CurrentLineNo / (double)Lines.Count;
				else
					return 0;
			}
		}
		public int CurrentLineNo
		{
			get { return _currentLineNo; }
			set
			{
				_currentLineNo = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged("ProgressPercentage");
			}
		}

		/// <summary>
		/// Gets the chapter number. e.g. the 3rd Chapter
		/// </summary>
		/// <value>
		/// The chapter no.
		/// </value>
		public ContentSectionViewModel()
		{
			IsLoading = false;
			Lines = new ObservableCollection<LineViewModel>();
		}

		public BookmarkInfo CreateBookmarkFromCurrentPage()
		{
			var imageLine = Lines.FirstOrDefault(line => line.IsImage);
			var bookmark = new BookmarkInfo
			{
				ViewDate = DateTime.Now,
				Position = new NovelPositionIdentifier
				{
					ChapterId = ChapterId,
					ChapterNo = ChapterNo,
					VolumeId = ParentVolumeId,
					VolumeNo = 0,//App.CurrentVolume.VolumeNo,
					SeriesId = ParentSeriesId,//App.CurrentSeries.Id,
					LineNo = CurrentLineNo,
				},
				ChapterTitle = Title,
				VolumeTitle = "",//App.CurrentVolume.Title,
				SeriesTitle = "",//App.CurrentSeries.Title,
				Progress = ProgressPercentage,
			};

			if (imageLine != null)
				bookmark.DescriptionImageUri = imageLine.Content;
			else
				bookmark.DescriptionImageUri = null;
			var textLines = from line in Lines where !line.IsImage && line.No >= CurrentLineNo && line.No <= CurrentLineNo + 2 select line.Content;
			var builder = new StringBuilder();
			bookmark.ContentDescription = textLines.Aggregate(builder, (b, s) => { b.AppendLine(s); return b; }).ToString();

			//bookmark.DescriptionImageUri = (from line in Lines where line.IsImage && line.Id < CurrentLineNo select line.Content).FirstOrDefault(),

			//if (bookmark.DescriptionImageUri == null) 
			//    bookmark.;

			return bookmark;
		}

		public async Task LoadCommentListAsync()
		{
			var commentedLines = await LightKindomHtmlClient.GetCommentedLinesListAsync(ChapterId);
			foreach (var lId in commentedLines)
			{
				if (Lines[lId - 1].No != lId)
					Debug.WriteLine("Can't find explicit comment line");
				Lines[lId - 1].MarkAsCommented();
			}
		}

		public async Task LoadCommentsAsync(LineViewModel lineView)
		{
			if (lineView.Comments.Count != 0)
				return;
			string lineId = lineView.No.ToString();

			Debug.WriteLine("Loading Comments : line_id = " + lineId + " ,chapter_id = " + ChapterId);
			try
			{
				lineView.IsLoading = true;
				var comments = await LightKindomHtmlClient.GetCommentsAsync(lineId, ChapterId);
				foreach (var comment in comments)
				{
					lineView.Comments.Add(new Comment(comment));
				}
				lineView.IsLoading = false;
			}
			catch (Exception)
			{
				Debug.WriteLine("Comments load failed : line_id = " + lineId + " ,chapter_id = " + ChapterId);
			}
		}

		public async Task LoadDataAsync(string id, bool loadCommentsList)
		{
			//await DataCache.ClearAll();
			if (this.ChapterId == id) return;
			IsLoading = true;
			ChapterId = id;
			Chapter chapter = null;
			try
			{
				chapter = await CachedClient.GetChapterAsync(id);
			}
			catch (Exception exception)
			{
				IsLoading = false;
				ChapterId = null;
				throw exception;
				//MessageBox.Show("Network issue occured, please check your wi-fi or data setting and try again.\nError Code:" + exception.Message, "Network issue", MessageBoxButton.OK);
			}
			if (chapter != null)
				DataContext = chapter;
			if (!loadCommentsList)
			{
				IsLoading = false;
				return;
			}
			try
			{
				await LoadCommentListAsync();
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Failed to retrive comment data : " + exception.Message);
				//MessageBox.Show(exception.Message, "Failed to retrive comment data.", MessageBoxButton.OK);
			}
			IsLoading = false;
		}

		public void Load(Chapter chapter)
		{
			DataContext = chapter;
		}

		private void LoadData(Chapter chapter)
		{
			if (chapter == null)
				throw new ArgumentNullException("chapter");
			Title = chapter.Title;
			NextChapterId = chapter.NextChapterId;
			PrevChapterId = chapter.PrevChapterId;
			ParentVolumeId = chapter.ParentVolumeId;
			ParentSeriesId = chapter.ParentSeriesId;
			ChapterNo = chapter.ChapterNo;
			Lines.Clear();
			foreach (var line in chapter.Lines)
			{
				if (line == null)
				{
					System.Diagnostics.Debug.WriteLine("null Lines in chapter data");
					continue;
				}
				var lv = new LineViewModel(line, chapter.Id);
				if (lv != null)
					Lines.Add(lv);
			}
		}
		public async Task LoadNextChapterAsync(bool loadCommentsList)
		{
			if (!IsLoading && !String.IsNullOrEmpty(NextChapterId))
				await LoadDataAsync(NextChapterId, loadCommentsList);
		}
		internal async Task LoadPrevChapterAsync(bool loadCommentsList)
		{
			if (!IsLoading && !String.IsNullOrEmpty(PrevChapterId))
			{
				await LoadDataAsync(PrevChapterId, loadCommentsList);
				CurrentLineNo = Lines[Lines.Count - 1].No;
			}
		}

		private ObservableCollection<LineViewModel> _lines;
		private bool _isLoading;
		private int _currentLineNo;
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public string ChapterId { get; set; }
		public string NextChapterId { get; set; }
		public string PrevChapterId { get; set; }
		public string ParentVolumeId { get; set; }
		public string ParentSeriesId { get; set; }

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
				if (Uri.IsWellFormedUriString(_content, UriKind.Absolute))
					return LineContentType.ImageContent;
				return LineContentType.TextContent;
			}
		}

		public int No { get; set; }

		public LineViewModel()
		{
			_content = null;
		}

		public LineViewModel(Line line,string chapterId)
		{
			//if (line.ContentType == LineContentType.TextContent)
			//	_content = line.Content; // Add the indent
			//else
			_content = line.Content;
			ParentChapterId = chapterId;
			No = line.No;
		}

		public LineViewModel(string content)
		{
			_content = content;
		}
		public LineViewModel(int id, string content)
		{
			No = id;
			_content = content;
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
		}

		public async Task<bool> AddCommentAsync(string commentText)
		{
			if (!string.IsNullOrEmpty(commentText) && commentText.Length < 300 && !String.IsNullOrEmpty(ParentChapterId))
			{
				if (HasNoComment)
					MarkAsCommented();
				Comments.Add(new Comment(commentText));
				return await LightKindomHtmlClient.CreateCommentAsync(No.ToString(), ParentChapterId, commentText);
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
			get { return _content.StartsWith("http"); }
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
			if (HasNoComment || String.IsNullOrEmpty(ParentChapterId)) 
				return;
			if (LoadCommentTask == null && Comments.Count == 0)
			{
				string lineId = No.ToString();
				Debug.WriteLine("Loading Comments : line_id = " + lineId + " ,chapter_id = " + ParentChapterId);
				LoadCommentTask = LightKindomHtmlClient.GetCommentsAsync(lineId, ParentChapterId);
				try
				{
					IsLoading = true;
					var comments = await LoadCommentTask;
					foreach (var comment in comments)
					{
						Comments.Add(new Comment(comment));
					}
					LoadCommentTask = null;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Comments load failed : line_id = " + lineId + " ,chapter_id = " + ParentChapterId);
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