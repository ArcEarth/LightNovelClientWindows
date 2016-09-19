using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Foundation;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LightNovel.Data
{
    public class Descriptor
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    public enum BookCatalog
    {
        Fantasy = 1,
        Fight = 2,
        Love = 3,
        AnotherWorld = 4,
        Commedy = 5,
        Daily = 6,
        School = 7,
        Herem = 8,
        Cliffhang = 9,
        Sifi = 10,
        Healing = 11,
        Superman = 12,
        Bad = 13,
        Monster = 14,
        Horor = 15,
        Siscon = 16,
        Fakebody = 17,
        All = 99,
    }

    public enum BookState
    {
        Serialing,
        Finished
    }

    public enum BookItemType
    {
        Series,
        Volume,
        Chapter,
    }
    public class BookItem
    {
        public string Source { get; set; }
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
                return BookItemType.Series;
                //if (HyperLinkUri.Contains("vollist"))
                //	return BookItemType.Series;
                //if (HyperLinkUri.Contains("book"))
                //	return BookItemType.Volume;
                //return BookItemType.Chapter;
            }
        }
    }

    public interface IDocumentSection
    {
        string Id { get; }
        string Path { get; }
        string Title { get; }
        string Description { get; }
        string CoverImage { get; }
        DateTime UpdateTime { get; }

        IDocumentSection Parent { get; }
        string Content { get; }
        IList<IDocumentSection> Children { get; }

        bool IsLoaded { get; }
        IAsyncAction GetAsync();
    }

    public interface IDocumentProperties
    {
        // Document meta data
        string DataProvider { get; }
        string Author { get; }
        string Illustrator { get; }
        string Publisher { get; }
        // A hash value which could differiate it with other novels
        string UniqueID { get; }
        ICollection<BookCatalog> Catalogs { get; }
        ICollection<string> Alias { get; }
    }


    public interface IDocument : IDocumentSection, IDocumentProperties
    {
    }

    public abstract class DocumentSectionBase : IDocumentSection
    {
        protected List<IDocumentSection> _children;

        public IList<IDocumentSection> Children => _children;

        public string Content { get; set; }

        public string CoverImage { get; set; }

        public string Description { get; set; }

        public string Id { get; set; }
        public string Path { get; set; }

        public IDocumentSection Parent { get; protected set; }

        public string Title { get; set; }

        public DateTime UpdateTime { get; set; }

        public bool IsLoaded
        {
            get; protected set;
        }

        public IAsyncAction GetAsync() => GetAsyncImpl().AsAsyncAction();

        public abstract Task GetAsyncImpl();
    }

    public abstract class DocumentBase : DocumentSectionBase, IDocument
    {
        protected Collection<BookCatalog> _catalogs = new Collection<BookCatalog>();
        protected Collection<string> _alias;

        public string DataProvider { get; set; }
        public string Author { get; set; }
        public string Illustrator { get; set; }
        public string Publisher { get; set; }
        // A hash value which could differiate it with other novels
        public string UniqueID { get; set; }
        public ICollection<BookCatalog> Catalogs => _catalogs;
        public ICollection<string> Alias => _alias;
    }

    public class ExtendedBookItem : BookItem/*, IDocumentProperties*/
    {
        // Contributer
        public string Author { get; set; }
        public string Illustrator { get; set; }
        public string Publisher { get; set; }
        public string Description { get; set; }
        public DateTime UpdateTime { get; set; }

        public IList<BookCatalog> Catalogs { get; set; }
        public IList<string> Alias { get; set; }
        public List<KeyValuePair<string, List<string>>> Volumes { get; set; }
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
        public string Provider { get; set; }
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

    public static class Convert
    {
        static HashAlgorithm hasher = MD5.Create();

        static readonly char[] padding = { '=' };
        public static string ToBase64UrlEncoding(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes)
                    .TrimEnd(padding).Replace('+', '-').Replace('/', '_');
        }

        public static byte[] FromBase64UrlEncoding(string encoded)
        {
            string incoming = encoded
                .Replace('_', '/').Replace('-', '+');
            switch (encoded.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            return System.Convert.FromBase64String(incoming);
        }

        public static string MD5Hash(string str)
        {
            var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(str));
            return ToBase64UrlEncoding(hash);
        }
    }

    public class Line
    {
        public static bool IsImageUri(string line)
        {
            Uri uri;
            if (!Uri.TryCreate(line, UriKind.Absolute, out uri))
                return false;
            var ext = Path.GetExtension(uri.LocalPath);
            return (!String.IsNullOrEmpty(ext) && (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif"));
        }

        //public static int GetIntQuery(string line, string key)
        //{
        //    var qidx = line.LastIndexOf('?');
        //    if (qidx == -1) return 0;
        //    line.IndexOf(key + "=");
        //}

        string _uid; // an MD5 hash of this line's content
        public string Uid
        {
            get
            {
                if (String.IsNullOrEmpty(_uid))
                    _uid = Convert.MD5Hash(Content);
                return _uid;
            }
        }
        public int No { get; set; }
        public string Content { get; set; }
        public LineContentType ContentType => IsImageUri(Content) ? LineContentType.ImageContent : LineContentType.TextContent;
        public int Width { get; set; }
        public int Height { get; set; }
        public ulong Size { get; set; }

        public bool ShouldSerializeLineContentType() => (ContentType == LineContentType.ImageContent);
        public bool ShouldSerializeWidth() => (ContentType == LineContentType.ImageContent);
        public bool ShouldSerializeHeight() => (ContentType == LineContentType.ImageContent);
        public bool ShouldSerializeSize() => (ContentType == LineContentType.ImageContent);
    }

    public struct Comment
    {
        public string cn;
        public string u;
    };

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

        public NovelPositionIdentifier Position
        {
            get
            {
                NovelPositionIdentifier pos = new NovelPositionIdentifier();
                pos.ChapterId = Id;
                pos.ChapterNo = ChapterNo;
                pos.VolumeId = ParentVolumeId;
                pos.SeriesId = ParentSeriesId;
                return pos;
            }
        }
    }

    public class Chapter : ChapterProperties
    {
        public string ErrorMessage { get; set; }
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
        public string CustomizedCoverImageUri { get; set; }

        [JsonIgnore]
        public Uri CoverImageUri
        {
            get
            {
                if (!String.IsNullOrEmpty(DescriptionThumbnailUri))
                    return new Uri(DescriptionThumbnailUri);
                else if (!String.IsNullOrEmpty(DescriptionImageUri))
                    return new Uri(DescriptionImageUri);
                else
                    return null;
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
