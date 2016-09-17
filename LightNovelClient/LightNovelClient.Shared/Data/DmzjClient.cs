using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using System.Text.RegularExpressions;
using System.Net;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;

namespace LightNovel.Data
{
    public abstract class DmzjDocSecBase : DocumentSectionBase
    {
        public static string Domain = "http://xs.dmzj.com/";
        public static string DomainId = "dmzj";
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240";
        public static Dictionary<string, List<Chapter>> vol_cache = new Dictionary<string, List<Chapter>>();
        static StorageFolder DmzjFolder;


        public static HttpClient NewHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
            return client;
        }

        public async static Task<HtmlDocument> GetHtmlDocumentAsync(Uri uri)
        {
            using (var client = NewHttpClient() /*new System.Net.Http.HttpClient()*/)
            {
                try
                {
                    var stream = (await client.GetInputStreamAsync(uri)).AsStreamForRead();
                    var doc = new HtmlDocument();
                    doc.Load(stream);
                    return doc;
                }
                finally
                {
                }
            }
        }

        private static IEnumerable<BookItem> ParseRecentBookItems(HtmlNode indexPage)
        {
            var newests = indexPage.FirstDescendantClass("column_newest_text").Element("ul").Elements("li");
            return newests.Select(li =>
            {
                var a = li.Element("a");
                var da = li.Descendants("a").Last();
                var did = da.GetAttributeValue("href", "").Split(new char[] { '/', '\\', '.' }, StringSplitOptions.RemoveEmptyEntries);

                return new BookItem
                {
                    Source = Domain,
                    SeriesId = did[0],
                    VolumeId = did[1],
                    Id = did[2],

                    Subtitle = da.GetAttributeValue("title", ""),
                    Title = a.GetAttributeValue("title", "#"),
                    CoverImageUri = a.Element("img").GetAttributeValue("src", ""),
                };

            });
        }

        public async static Task<IDictionary<string, IList<BookItem>>> GetFeaturedBooks()
        {
            Uri uri = new Uri(Domain + "update_1.shtml");
            var doc = await GetHtmlDocumentAsync(uri);
            if (doc == null) return null;

            Dictionary<string, IList<BookItem>> bookLists = new Dictionary<string, IList<BookItem>>();
            var recents = ParseRecentBookItems(doc.DocumentNode).ToList();
            bookLists.Add("Recent", recents);

            return bookLists;
        }

        public static async Task<Chapter> GetChapterAsync(string chpId, Volume vol)
        {
            List<Chapter> chpts;
            var vid = vol.Id;
            if (!vol_cache.TryGetValue(vid, out chpts))
            {
                //http://xs.dmzj.com/2014/7336/7336.txt
                if (DmzjFolder == null)
                    DmzjFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync(DomainId, CreationCollisionOption.OpenIfExists);

                var serfolder = await DmzjFolder.CreateFolderAsync(vol.ParentSeriesId, CreationCollisionOption.OpenIfExists);

                IInputStream stream = null;
                try
                {
                    var file = await serfolder.GetFileAsync(vid + ".txt");
                    stream = await file.OpenSequentialReadAsync();
                }
                catch
                {
                    var uri = new Uri(Domain + vol.ParentSeriesId + '/' + vid + '/' + vid + ".txt");
                    using (var client = NewHttpClient())
                        stream = await client.GetInputStreamAsync(uri);
                    var memstream = new MemoryStream();
                    var file = await serfolder.CreateFileAsync(vid + ".txt", CreationCollisionOption.ReplaceExisting);
                    var os = await file.OpenStreamForWriteAsync();
                    await stream.AsStreamForRead().CopyToAsync(memstream);
                    memstream.Seek(0, SeekOrigin.Begin);
                    await memstream.CopyToAsync(os);
                    memstream.Seek(0, SeekOrigin.Begin);
                    stream = memstream.AsInputStream();
                }

                chpts = ParseVolumnText(stream.AsStreamForRead(), vol.Chapters);
                vol_cache.Add(vid, chpts);
            }
            var chpt = chpts.FirstOrDefault(c => c.Id == chpId);
            if (chpt.Title == "插画" || chpt.Title == "插图") // there is an bug in dmzj's image address
            {
                // http://xs.dmzj.com/1464/8046/66927.shtml
                var uri = Domain + vol.ParentSeriesId + '/' + vol.Id + '/' + chpt.Id + ".shtml";
                var doc = await GetHtmlDocumentAsync(new Uri(uri));
                var contents = doc.DocumentNode.FirstDescendantClass("novel_text");
                int lino = 0;
                chpt.Lines = contents.Descendants("img").Select(img => new Line(lino++, LineContentType.ImageContent, img.GetAttributeValue("src", null))).ToList();
                foreach (var line in chpt.Lines)
                {
                    var fullurl = Domain + line.Content;
                    var imguri = new Uri(fullurl);
                    line.Content = imguri.AbsoluteUri;
                }
            }
            return chpt;
        }

        public static bool IsImageUri(string uri)
        {
            return Uri.IsWellFormedUriString(uri, UriKind.Absolute) &&
                   uri.EndsWith(".jpg") || uri.EndsWith(".jepg") || uri.EndsWith(".png") || uri.EndsWith(".gif");
        }

        private static List<Chapter> ParseVolumnText(Stream stream, IList<ChapterProperties> properties)
        {
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            List<Chapter> chpts = new List<Chapter>();
            for (int i = 0; i < properties.Count; i++)
            {
                var cp = properties[i];
                string nxtTitle = null;
                if (i < properties.Count - 1) nxtTitle = properties[i + 1].Title;
                var chpt = new Chapter();
                chpt.Title = cp.Title;
                chpt.Id = cp.Id;
                var lines = new List<Line>();
                int no = 0;
                for (string line = reader.ReadLine();
                    !reader.EndOfStream && (nxtTitle == null || !line.StartsWith(nxtTitle));
                    line = reader.ReadLine())
                {
                    // bypass invaliad lines
                    if (String.IsNullOrWhiteSpace(line)
                        || line.StartsWith("小说插图")
                        || line.StartsWith("<script"))
                        continue;
                    var type = IsImageUri(line) ? LineContentType.ImageContent : LineContentType.TextContent;
                    lines.Add(new Line(no++, type, line));
                }
                chpt.Lines = lines;
                chpts.Add(chpt);
            }
            return chpts;
        }

        //public async Task<Volume> GetVolumeAsync(string vid, bool forceRefresh = false)
        //{
        //    //http://xs.dmzj.com/2014/7336/7336.txt
        //    var uri = new Uri(domain + vid + '/' + vid + ".txt");
        //    using (var client = new HttpClient())
        //    {
        //        var content = await client.GetStringAsync(uri);
        //    }
        //}


        private static string RemoveLabel(string p)
        {
            p = p.Replace("\t", "");
            p = p.Trim();
            var s = p.Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Length > 1)
                return String.Concat(s[s.Length - 1]);
            else
                return s[0];
            //p = Regex.Replace(p, @"第\d+?[章卷话]", "");
            ////p = Regex.Replace(p, @"第\d+?", "");
            //p = p.Trim();
            //return p;
        }
        private static string RemoveLabel2(string p)
        {
            p = p.Replace("\t", "");
            p = p.Trim();
            var s = p.Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (s.Length > 1)
                return s[s.Length - 1];
            else
                return s[0];
        }

        private static string ExtractLabel(string p)
        {
            p = p.Replace("\t", "");
            p = p.Trim();
            var strs = p.Split(new char[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var s = strs[0].Trim();
            var match = Regex.Match(s, @"第?(.+?)[章卷话]");
            if (match.Success)
                return match.Result("$1");
            else
                return null;
        }

        public static async Task<Series> GetSeriesAsync(string sid)
        {
            //http://xs.dmzj.com/2014/index.shtml
            var uri = new Uri(Domain + sid + "/index.shtml");
            HtmlDocument doc = await GetHtmlDocumentAsync(uri);
            Series ser = new Series();
            ser.Id = sid;
            var root = doc.DocumentNode;
            // .novel_cover
            var cover = root.Descendants().First(node => node.HasClass("novel_cover"));
            // .novel_cover_text
            var info = cover.NextSiblingClass("novel_cover_text");
            // .download_rtx
            var volumns = root.Descendants("div").Where(div => div.HasClass("download_rtx"));

            cover = cover.Descendants("img").First();
            ser.CoverImageUri = cover.GetAttributeValue("src", "");
            ser.Id = sid;
            ser.Title = cover.GetAttributeValue("title", "#");
            int vno = 0;
            ser.Volumes = volumns.Select(vnode =>
            {
                var vol = new Volume();
                vol.CoverImageUri = ser.CoverImageUri;
                vol.ParentSeriesId = sid;
                vol.VolumeNo = vno++;
                var vtitle = vnode.Element("ol").Element("li").LastChild.InnerText;
                vol.Label = ExtractLabel(vtitle);
                vol.Title = RemoveLabel(vtitle);
                vol.Id = vnode.Element("ol").Descendants("a").First().GetAttributeValue("id", "");
                int cno = 0;
                vol.Chapters = vnode.Element("ul").Elements("li").Select(
                    cnode =>
                    {
                        var chpt = new ChapterProperties();
                        cnode = cnode.Element("a");
                        chpt.Title = cnode.GetAttributeValue("title", "##");
                        var cid = cnode.GetAttributeValue("href", "");
                        cid = cid.Substring(cid.LastIndexOf('/') + 1);
                        cid = cid.Substring(0, cid.Length - ".sthml".Length);
                        chpt.Id = cid;
                        chpt.ParentVolumeId = vol.Id;
                        chpt.ParentSeriesId = ser.Id;
                        chpt.ChapterNo = cno++;
                        return chpt;
                    }
                    ).ToList();
                return vol;
            }).ToList();
            return ser;
        }
    }

    public class DmzjSeries : DmzjDocSecBase
    {
        public override async Task GetAsyncImpl()
        {
            if (IsLoaded) return;
            var series = GetSeriesAsync(this.Id);
            this._children = new List<IDocumentSection>();
        }
    }

    public class DmzjVolumn : DmzjDocSecBase
    {
        public override async Task GetAsyncImpl()
        {
            if (IsLoaded) return;

            var vid = this.Id;
            var sid = this.Parent.Id;

            //example: http://xs.dmzj.com/2014/7336/7336.txt
            var uri = new Uri(Domain + sid + '/' + vid + '/' + vid + ".txt");
            using (var client = NewHttpClient())
            {
                this.Content = await client.GetStringAsync(uri);
            }

            IsLoaded = true;
        }
    }
}
