using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace LightNovel.Service
{
	public class Descriptor
	{
		public string Id { get; set; }
		public string Title { get; set; }
	}

	public enum BookItemType
	{
		Series,
		Volume,
		Chapter,
	}
	public class BookItem
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public string Subtitle { get; set; }
		public string VolumeNo { get; set; }
		public string SeriesId { get; set; }
		public string VolumeId { get; set; }
		public string HyperLinkUri { get; set; }
		public string CoverImageUri { get; set; }

		[JsonIgnore]
		public BookItemType ItemType
		{
			get
			{
				if (HyperLinkUri.Contains("vollist"))
					return BookItemType.Series;
				if (HyperLinkUri.Contains("book"))
					return BookItemType.Volume;
				return BookItemType.Chapter;
			}
		}
	}

	public class FavourVolume
	{
		public string FavId { get; set; }
		public string VolumeId { get; set; }
		public string SeriesTitle { get; set; }
		public string VolumeNo { get; set; }
		public string VolumeTitle { get; set; }
		public DateTime FavTime { get; set; }
		public string CoverImageUri { get; set; }
		public string Description { get; set; }
	}

	public class Volume
	{
		public string Id { get; set; }
		public string Label { get; set; }
		public int VolumeNo { get; set; }
		public string Title { get; set; }
		public DateTime UpdateTime { get; set; }
		public string Description { get; set; }
		public string CoverImageUri { get; set; }

		// Contributer
		public string Author { get; set; }
		public string Illustrator { get; set; }
		public string Publisher { get; set; }

		// Translation Contributers
		public string Translator { get; set; }
		public string Proofreader { get; set; }
		public string SourceProvider { get; set; }
		public string PictureEditor { get; set; }

		//public Descriptor ParentSeriesDescriptor { get; set; }
		//public IEnumerable<Descriptor> ChapterDescriptorList { get; set; }

		//public Series ParentSeries
		//{
		//    get
		//    {
		//        return LightNovelService.Series(ParentSeriesId);
		//    }
		//}
		public IList<ChapterProperties> Chapters { get; set; }
		//public Volume NextVolume { get { return LightNovelService.Volume(NextVolumeId); } }
		//public Volume PrevVolume { get { return LightNovelService.Volume(PrevVolumeId); } }
		public string NextVolumeId { get; set; }
		public string PrevVolumeId { get; set; }
		public string ParentSeriesId { get; set; }
	}

	public class Series
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public DateTime UpdateTime { get; set; }
		public string Description { get; set; }
		public string CoverImageUri { get; set; }

		// Contributer
		public string Author { get; set; }
		public string Illustrator { get; set; }
		public string Publisher { get; set; }

		public IList<Volume> Volumes { get; set; }
	}

	public enum LineContentType
	{
		TextContent,
		ImageContent,
	}
	public class Line
	{
		public Line(int no, LineContentType type, string content)
		{
			No = no;
			Content = content;
			ContentType = type;
		}

		public int No { get; set; }
		public string Content { get; set; }
		public LineContentType ContentType { get; set; }
	}

	public interface IChapterProperties
	{
		string Id { get; set; }
		string Title { get; set; }
		int ChapterNo { get; set; }
		string NextChapterId { get; set; }
		string PrevChapterId { get; set; }
		string ParentVolumeId { get; set; }
		string ParentSeriesId { get; set; }
	}

	public class ChapterProperties : IChapterProperties
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public int ChapterNo { get; set; }
		public string NextChapterId { get; set; }
		public string PrevChapterId { get; set; }
		public string ParentVolumeId { get; set; }
		public string ParentSeriesId { get; set; }
	}

	public class Chapter : ChapterProperties
	{
		public IList<Line> Lines { get; set; }

		//public string Id { get; set; }
		//public string Title { get; set; }
		//public int ChapterNo { get; set; }
		//public string NextChapterId { get; set; }
		//public string PrevChapterId { get; set; }
		//public string ParentVolumeId { get; set; }
		//public string ParentSeriesId { get; set; }
	}
	//public enum NavigatorType
	//{
	//	Abusolute,
	//	Relative,
	//}
	public class NovelPositionIdentifier
	{
		//public uint SpecificLevel { get; set; }
		//public NavigatorType Type { get; set; }
		public string SeriesId { get; set; }
		public string VolumeId { get; set; }
		public int VolumeNo { get; set; }
		public string ChapterId { get; set; }
		public int ChapterNo { get; set; }
		public int LineNo { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this);
		}

		public static NovelPositionIdentifier Parse(string json)
		{
			return JsonConvert.DeserializeObject<NovelPositionIdentifier>(json);
		}
	}

	public class BookmarkInfo
	{
		public DateTime ViewDate { get; set; }
		public NovelPositionIdentifier Position { get; set; }
		public double Progress { get; set; }
		public string ContentDescription { get; set; }
		public string DescriptionImageUri { get; set; }
		public string ChapterTitle { get; set; }
		public string VolumeTitle { get; set; }
		public string SeriesTitle { get; set; }
		public string DescriptionThumbnailUri { get; set; }
		public bool IsDeleted { get; set; }

		[JsonIgnore]
		public Uri CoverImageUri
		{
			get
			{
				if (!String.IsNullOrEmpty(DescriptionThumbnailUri))
					return new Uri(DescriptionThumbnailUri);
				else 
					return new Uri(DescriptionImageUri);
			}
		}
		[JsonIgnore]
		public string Description
		{
			get
			{
				return ContentDescription;
			}
		}
	}

}
