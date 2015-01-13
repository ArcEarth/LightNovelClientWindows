using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Windows.Threading;
using System.Windows;
using Windows.Storage;
using Microsoft.Phone.Logging;
using Q42.WinRT.Data;
using System.Runtime.CompilerServices;
using LightNovel;
using LightNovel.Resources;
using LightNovel.Service;

namespace LightNovel.ViewModels
{
	public static class CachedClient
	{
		public static Task<Series> GetSeriesAsync(string id, bool forceRefresh = false)
		{
			return DataCache.GetAsync("series-" + id, () => LightKindomHtmlClient.GetSeriesAsync(id));
		}
		public static Task<Volume> GetVolumeAsync(string id, bool forceRefresh = false)
		{
			return DataCache.GetAsync("volume-" + id, () => LightKindomHtmlClient.GetVolumeAsync(id));
		}
		public static Task<Chapter> GetChapterAsync(string id, bool forceRefresh = false)
		{
			return DataCache.GetAsync("chapter-" + id, () => LightKindomHtmlClient.GetChapterAsync(id));
		}

		public static Task<List<Descriptor>> GetSeriesIndexAsync(bool forceRefresh = false)
		{
			return DataCache.GetAsync("series_index", () => LightKindomHtmlClient.GetSeriesIndexAsync());
		}

		public static Task<IList<KeyValuePair<string, IList<BookItem>>>> GetRecommandedBookLists()
		{
			return DataCache.GetAsync("popular_series", () => LightKindomHtmlClient.GetRecommandedBookLists(), DateTime.Now.AddDays(1));
		}

	}
	public class BookCoverViewModel : INotifyPropertyChanged
	{
		public BookCoverViewModel()
		{
		}

		public BookCoverViewModel(BookItem item)
		{
			Title = item.Title;
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
						return String.Format("/SeriesViewPage.xaml?id={0}&volume={1}","",Id);
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
		public string CoverImageUri { get; set; }
		public string Description { get; set; }
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
				if (String.IsNullOrEmpty(value))
					_description = AppResources.NoDescription;
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
			VolumeList = new ObservableCollection<VolumeViewModel>();
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
		public async Task LoadDataAsync(string serId, string volId = "", string cptId = "")
		{
			IsLoading = true;
			Id = serId;
			try
			{
				var series = await CachedClient.GetSeriesAsync(serId);
				DataContext = series;
			}
			catch (Exception exception)
			{
				IsLoading = false;
				Id = null;
				MessageBox.Show("Network issue occured, please check your wi-fi or data setting and try again.\nError Code:" + exception.Message, "Network issue", MessageBoxButton.OK);
			}
		}

		private void LoadData(Series series)
		{
			IsLoading = true;

			Title = series.Title;
			Author = series.Author;
			CoverImageUri = new Uri(series.CoverImageUri);
			Illustrator = series.Illustrator;
			VolumeList.Clear();
			foreach (var vol in series.Volumes)
			{
				var vvm = new VolumeViewModel
				{
					DataContext = vol
				};
				//await vvm.LoadDataAsync(vol.ID);
				//vvm.Description = vol.Description;
				//int no = 0;
				//foreach (var cp in vol.Chapters)
				//{
				//    var cpvm = new ChapterPreviewModel
				//    {
				//        Id = cp.Id,
				//        No = cp.ChapterNo,
				//        Title = cp.Title,
				//    };
				//    vvm.ChapterList.Add(cpvm);
				//}
				VolumeList.Add(vvm);
			}
			IsLoading = false;
			//await Task.WhenAll(VolumeList.Select(vvm => vvm.LoadDataAsync(vvm.Id)));
		}
		public ObservableCollection<VolumeViewModel> VolumeList { get; set; }

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
				if (value == "" || value == null)
					_description = AppResources.NoDescription;
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
			IsLoading = true;
			Id = id;
			try
			{
				var volume = await CachedClient.GetVolumeAsync(id);
				DataContext = volume;
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
				MessageBox.Show("Network issue occured, please check your wi-fi or data setting and try again.\nError Code:" + exception.Message, "Network issue", MessageBoxButton.OK);
			}
		}

		private void LoadData(Volume volume)
		{
			IsLoading = true;
			Id = volume.Id;
			Title = volume.Title;
			Author = volume.Author;
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
		public ObservableCollection<VolumeViewModel> VolumeList { get; set; }

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
	public class ChapterViewModel : INotifyPropertyChanged
	{
		private Chapter _dataConrtext;
		public Chapter DataContext
		{
			get { return _dataConrtext; }
			private set
			{
				if (_dataConrtext == value) return;
				LoadData(value);
				_dataConrtext = value;
				NotifyPropertyChanged();
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
		public ChapterViewModel()
		{
			IsLoading = false;
			Lines = new ObservableCollection<LineViewModel>();
		}

		public BookmarkInfo CreateBookmarkFromCurrentPage()
		{
			var bookmark = new BookmarkInfo
			{
				ViewDate = DateTime.Now,
				Position = new NovelPositionIdentifier
				{
					ChapterId = ChapterId,
					ChapterNo = ChapterNo,
					VolumeId = ParentVolumeId,
					VolumeNo = App.CurrentVolume.VolumeNo,
					SeriesId = App.CurrentSeries.Id,
					LineNo = CurrentLineNo,
				},
				ContentDescription =
					(from line in Lines where !line.IsImage && line.Id >= CurrentLineNo select line.Content)
						.FirstOrDefault(),
				DescriptionImageUri = App.CurrentVolume.CoverImageUri,
				ChapterTitle = Title,
				VolumeTitle = App.CurrentVolume.Title,
				SeriesTitle = App.CurrentSeries.Title,
				Progress = ProgressPercentage,
			};
			if (bookmark.ContentDescription == null)
			{
				bookmark.ContentDescription =
					(from line in Lines where !line.IsImage && line.Id < CurrentLineNo select line.Content)
						.LastOrDefault();
			}
			if (bookmark.ContentDescription == null)
				bookmark.ContentDescription = Title;
			//bookmark.DescriptionImageUri = (from line in Lines where line.IsImage && line.Id < CurrentLineNo select line.Content).FirstOrDefault(),

			//if (bookmark.DescriptionImageUri == null) 
			//    bookmark.;

			return bookmark;
		}
		//public void LoadSampleData()
		//{
		//    string NovelText;
		//    using (var reader = new System.IO.StreamReader("SampleData\\SampleNovel.txt"))
		//    {
		//        NovelText = reader.ReadToEnd();
		//    }
		//    System.IO.StringReader rawTextReader = new System.IO.StringReader(NovelText);
		//    string line;
		//    while ((line = rawTextReader.ReadLine()) != null && Lines.Count < 7000)
		//    {
		//        Lines.Add(new LineViewModel(line, "Add comments here"));
		//    }
		//}
		public async Task LoadCommentListAsync()
		{
			var commentedLines = await LightKindomHtmlClient.GetCommentedLinesListAsync(ChapterId);
			foreach (var lId in commentedLines)
			{
				if (Lines[lId - 1].Id != lId)
					Debug.WriteLine("Can't find explicit comment line");
				Lines[lId - 1].MarkAsCommented();
			}
		}

		public async Task LoadCommentsAsync(LineViewModel lineView)
		{
			if (lineView.Comments.Count != 0)
				return;
			string lineId = lineView.Id.ToString();

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
				MessageBox.Show("Network issue occured, please check your wi-fi or data setting and try again.\nError Code:" + exception.Message, "Network issue", MessageBoxButton.OK);
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
				//foreach (LineViewModel line in vm.Lines)
				//{
				//    if (line.HasComments && line.Comments.Count == 0 && !line.IsLoading)
				//    await ViewModel.LoadCommentsAsync(line);
				//}
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Failed to retrive comment data : " + exception.Message);
				//MessageBox.Show(exception.Message, "Failed to retrive comment data.", MessageBoxButton.OK);
			}
			IsLoading = false;
		}

		private void LoadData(Chapter chapter)
		{
			if (chapter == null)
				throw new ArgumentNullException("chapter");
			IsLoading = true;
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
				var lv = LineViewModel.Create(line);
				if (lv != null)
					Lines.Add(lv);
			}
			CurrentLineNo = 1;
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
				CurrentLineNo = Lines[Lines.Count - 1].Id;
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
		private string _content;

		public int Id { get; set; }

		public LineViewModel()
		{
			_content = null;
			//Comments = new ObservableCollection<Comment>();
		}

		public LineViewModel(string Line)
		{
			_content = Line;
			//Comments = new ObservableCollection<Comment>();
		}

		public LineViewModel(string Line, params string[] args)
		{
			_content = Line;
			var comments = args.Select(arg => new Comment(arg));
			Comments = new ObservableCollection<Comment>(comments);
			Comments.CollectionChanged += Comments_CollectionChanged;
		}

		public void MarkAsCommented()
		{
			if (Comments != null)
				return;
			Comments = new ObservableCollection<Comment>();
			Comments.CollectionChanged += Comments_CollectionChanged;
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

		public virtual bool IsImage
		{
			get { return false; }
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

		public event PropertyChangedEventHandler PropertyChanged;
		private ObservableCollection<Comment> _comments;
		private bool _isLoading;

		protected void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		internal static LineViewModel Create(Line line)
		{
			LineViewModel lvm = null;
			if (line.ContentType == LineContentType.TextContent)
				lvm = new LineViewModel(line.Content) { Id = line.No };
			else if (line.ContentType == LineContentType.ImageContent)
				lvm = new IllustrationViewModel(line.Content) { Id = line.No };
			return lvm;
		}
	}
	public class IllustrationViewModel : LineViewModel
	{
		private string _content;
		public override bool IsImage
		{
			get { return true; }
		}
		public override string Content
		{
			get { return _content; }
			set
			{
				_content = value;
				NotifyPropertyChanged("Content");
			}
		}

		public Uri NonCachedImageUri { get; set; }

		public bool IsLoading
		{
			get;
			private set;
		}
		public IllustrationViewModel()
		{
			IsLoading = false;
			Content = null;
		}

		public IllustrationViewModel(Uri imgSourceUri)
		{
			Content = null;
			IsLoading = true;
			NonCachedImageUri = imgSourceUri;
			var cacheingFileTask = WebDataCache.GetAsync(imgSourceUri);
			cacheingFileTask.ContinueWith(localUri =>
			{
				Deployment.Current.Dispatcher.BeginInvoke(() =>
				{
					if (localUri.Status != TaskStatus.RanToCompletion)
						Content = imgSourceUri.AbsolutePath;
					else
						Content = localUri.Result.Path;
					IsLoading = false;
				});
			});
		}

		public IllustrationViewModel(string ImgSourceUri)
			: this(new Uri(LightKindomHtmlClient.SeverBaseUri, ImgSourceUri))
		{ }
	}
}