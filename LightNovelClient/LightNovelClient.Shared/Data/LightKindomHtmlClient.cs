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
//using System.Net;
//using System.Net;

namespace LightNovel.Data
{
    public enum UserAgentType
    {
        None,
        IE11,
        Account,
    }

    public class Session
    {
        public Session()
        {
            Key = null;
            Expries = DateTime.Now.AddYears(-100);
        }
        public string Name { get; set; }
        public string Key { get; set; }
        public DateTime Expries { get; set; }
        public bool Expired
        {
            get
            {
                return DateTime.Now >= Expries;
            }
        }
    }
    public class NotSignedInException : Exception
    { }


    public static class LightKindomHtmlClient
    {
        //private static readonly Dictionary<string, Volume> VolumeDictionary = new Dictionary<string, Volume>();
        //private static readonly Dictionary<string, Series> SeriesDictionary = new Dictionary<string, Series>();
        //private static readonly Dictionary<string, Chapter> ChaptersDictionary = new Dictionary<string, Chapter>();

        private const string CharsetAffix = "?charset=big5";
        private static HashSet<string> BlackList = new HashSet<string> { "1277" };

        private const string SeverBasePath = "http://linovel.com";
        private const string ChapterSource = SeverBasePath + "/book/read?chapterId={0}&volId={1}&bookId={2}";
        private const string SeriesSource = SeverBasePath + "/book/detail?id={0}";

        private const string CommentListPath = SeverBasePath + "/api/getChapterComments?bookId={0}&chapterId={1}&line={2}"; // POST /chapterid
        private const string CommentSetPath = SeverBasePath + "/n/set_comment_content/"; // POST /chapterid

        private const string HomePageSource = SeverBasePath;// + "/main/index.html"; // Well ,it's Ok to use the base address directly
        private const string SeriesIndexSource = SeverBasePath + "/book/pinyin";
        private const string RecommandPath = SeverBasePath + "/api/getRecommendBooks";
        private const string LastesUpdatedPath = SeverBasePath + "/api/getLatestBooks";
        private const string RecentPublishedPath = SeverBasePath + "/api/getNewPubBooks";
        private const string CatalogPath = SeverBasePath + "api/getBooksByCat?cat={0}&page={1}";

        private const string QueryPath = SeverBasePath + "/search/?keyword={0}";

        private const string UserLoginPath = SeverBasePath + "/main/user_login_withQuestion.html";
        private const string UserLoginInterfacePath = SeverBasePath + "/main/login.html";
        private const string UserLogoutPath = SeverBasePath + "/main/user_logout.html";
        private const string UserRecentPath = SeverBasePath + "/user/index.html";
        private const string UserFavourPath = SeverBasePath + "/user/favour.html";
        private const string UserGetFavourPath = SeverBasePath + "/user/get_user_favourite.html?page=1";
        private const string UserDeleteFavoritePath = SeverBasePath + "/user/delete_user_favourite.html"; // Post {fav_id[] : ids}
        private const string UserDeleteFavoriteBatchPath = SeverBasePath + "/user/delete_user_favourite_batch.html";
        private const string UserAddFavoritePath = SeverBasePath + "/main/add_favourite.html"; // Post {vol_id : id}
        private const string UserAddFavoriteSeriesPath = SeverBasePath + "/main/add_batch_favourite.html"; // Post { series_id : id}
        private const string UserSetBookmarkPath = SeverBasePath + "/main/set_bookmark.html";

        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.135 Safari/537.36 Edge/12.10240";
        private static string accounrUserAgent = DefaultUserAgent;
        private const string CiSession = "ci_session_3";
        public static string AccountUserAgent
        {
            get { return accounrUserAgent; }
            set { accounrUserAgent = value; }
        }

        public readonly static Uri SeverBaseUri = new Uri(SeverBasePath);

        public readonly static Uri SeriesIndexSourceUri = new Uri(SeriesIndexSource);
        public readonly static Uri RecommandUri = new Uri(RecommandPath);
        public readonly static Uri LastesUpdatedUri = new Uri(LastesUpdatedPath);
        public readonly static Uri RecentPublishedUri = new Uri(RecentPublishedPath);

        public readonly static Uri CommentSetUri = new Uri(CommentSetPath);
        public readonly static Uri HomePageSourceUri = new Uri(HomePageSource);// + "/main/index.html"; // Well ,it's Ok to use the base address directly
        public readonly static Uri UserLoginUri = new Uri(UserLoginPath);
        public readonly static Uri UserLoginInterfaceUri = new Uri(UserLoginInterfacePath);
        public readonly static Uri UserLogoutUri = new Uri(UserLogoutPath);
        public readonly static Uri UserRecentUri = new Uri(UserRecentPath);
        public readonly static Uri UserFavourUri = new Uri(UserFavourPath);
        public readonly static Uri UserGetFavourUri = new Uri(UserGetFavourPath);
        public readonly static Uri UserDeleteFavoriteUri = new Uri(UserDeleteFavoritePath); // Post {fav_id[] : ids}
        public readonly static Uri UserDeleteFavoriteBatchUri = new Uri(UserDeleteFavoriteBatchPath); // Post {fav_id[] : ids}
        public readonly static Uri UserAddFavoriteUri = new Uri(UserAddFavoritePath); // Post {vol_id : id}
        public readonly static Uri UserAddFavoriteSeriesUri = new Uri(UserAddFavoriteSeriesPath); // Post { series_id : id}
        public readonly static Uri UserSetBookmarkUri = new Uri(UserSetBookmarkPath);

        public static bool UseSimplifiedCharset { get; set; } = true;

        private static HttpBaseProtocolFilter _fileter = new HttpBaseProtocolFilter();
        private static HttpCookie _CiSessionCookie;
        private static Session _ci_session;

        public static Session Credential
        {
            get { return _ci_session; }
            set
            {
                if (_ci_session == value)
                    return;
                if (value == null)
                {
                    _ci_session = value;//null
                    _fileter.CookieManager.DeleteCookie(_CiSessionCookie);
                    _CiSessionCookie = null;
                    return;
                }
                else if (!value.Expired)
                {
                    _ci_session = value;
                    var cookie = new HttpCookie(CiSession, SeverBasePath, "/");
                    cookie.Value = _ci_session.Key;
                    _CiSessionCookie = cookie;
                    //_fileter.CookieManager.SetCookie(cookie);

                    //cookie.Domain = "www.linovel.com";
                    //var header = _cookieContainer.GetCookieHeader(SeverBaseUri);
                    //Debug.WriteLine(header);
                }
                else
                {
                    Debug.WriteLine("Session Expired!");
                }
            }
        }
        public static bool IsSignedIn
        {
            get { return _CiSessionCookie != null; }
        }

        public static string UserName
        {
            get;
            private set;
        }

        public static System.Net.Http.HttpClient NewDefaultHttpClient()
        {
            var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
            return client;
        }

        public static HttpClient NewUserHttpClient()
        {
            HttpClient client = null;
            if (_ci_session != null && !_ci_session.Expired)
            {
                var filter = new HttpBaseProtocolFilter();
                filter.CookieManager.GetCookies(SeverBaseUri);
                //filter.CookieManager.SetCookie(_CiSessionCookie);
                client = new HttpClient(filter);
            }
            else
            {
                client = new HttpClient();
            }

            if (!string.IsNullOrEmpty(accounrUserAgent))
                client.DefaultRequestHeaders.Add("User-Agent", accounrUserAgent);
            else
                client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);

            return client;
        }
        public static async Task LogoutAsync()
        {
            using (var client = NewUserHttpClient())
            {
                await client.PostAsync(UserLogoutUri, new HttpStringContent(""));
                Credential = null;
            }
        }

        public static async Task<bool> ValidateLoginAuth()
        {
            using (var client = NewUserHttpClient())
            {
                var resp = await client.GetAsync(UserLoginInterfaceUri);
                var html = await resp.Content.ReadAsStringAsync();
                return !html.Contains("/userauth/index.html") && html.Contains("lk-login");
            }
        }

        public static async Task<Session> LoginAsync(string userName, string passWord, string questionId = "0", string answer = "")
        {
            {
                var filter = new HttpBaseProtocolFilter();
                var cookies = filter.CookieManager.GetCookies(SeverBaseUri);
                if (cookies.Count > 0)
                {
                    await LogoutAsync();
                }
            }
            //if (!await ValidateLoginAuth())
            //    return null;
            using (var client = NewUserHttpClient())
            {

                var content = new HttpFormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("user_name",userName),
                    new KeyValuePair<string,string>("pass_word",passWord),
                    new KeyValuePair<string,string>("questionid",questionId),
                    new KeyValuePair<string,string>("answer",answer),
                });

                var resp = await client.PostAsync(UserLoginUri, content);
                var respString = await resp.Content.ReadAsStringAsync();
                JObject loginResult = JObject.Parse(respString);

                if (loginResult["message"] == null || (bool)loginResult["message"] == false)
                {
                    throw new Exception("Invailid user name or password");
                    //return null; // Failed, means that the user/password is incorrect
                }

                // This behavier is different for Phone and PC!!!
                var raw = resp.Headers["Set-cookie"];
                //string validCookie;
                ////string[] sperator = {"ci_session="};
                ////raw.Split(sperator,5,StringSplitOptions.RemoveEmptyEntries);

                //// Total 3, last second is the second = =|||
                //var begin = raw.IndexOf("ci_session=", raw.IndexOf("ci_session=", 11) + 11); // find the forth occurence of "ci_session="
                //var end = raw.IndexOf("path=/", begin + 11) + 5;
                //validCookie = raw.Substring(begin, end - begin + 1);
                //var doc = await client.GetStringAsync(UserRecentPath);

                //validCookie = validCookie.Substring(11, validCookie.IndexOf(';') - 11);
                //Credential = new Session { Key = validCookie, Expries = DateTime.Now.AddDays(1) /*_cookieContainer.GetCookies(SeverBaseUri)[CiSession].Expires*/ };

                var filter = new HttpBaseProtocolFilter();
                var cookies = filter.CookieManager.GetCookies(SeverBaseUri);
                var cookie = cookies.FirstOrDefault(c => c.Name.Contains("ci_session"));
                if (cookie == null)
                    throw new Exception("Didn't get proper login token!");
                Credential = new Session { Name = cookie.Name, Key = cookie.Value, Expries = cookie.Expires.Value.DateTime };
                //var doc = await client.GetStringAsync(UserRecentPath);

            }
            return Credential;
        }

        public async static Task<HtmlDocument> GetHtmlDocumentAsync(Uri uri)
        {
            using (var client = NewUserHttpClient() /*new System.Net.Http.HttpClient()*/)
            {
                //client.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);
                try
                {
                    //var stream = await client.GetStreamAsync(uri);
                    var stream = (await client.GetInputStreamAsync(uri)).AsStreamForRead();
                    var doc = new HtmlDocument();
                    doc.Load(stream);
                    return doc;
                }
                finally
                {
                }
            }
            return null;
        }

        public static async Task<IEnumerable<Descriptor>> GetUserRecentViewedVolumesAsync()
        {
            using (var client = NewUserHttpClient())
            {
                try
                {
                    var stream = await client.GetInputStreamAsync(UserRecentUri);

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.Load(stream.AsStreamForRead());
                    //node => node.PreviousSublingElement("strong").InnerText.Contains("卷浏览记录")
                    var volNodes = htmlDoc.DocumentNode.Descendants("ul").FirstOrDefault(node => node.HasClass("unstyled mt-10"));
                    var vols = volNodes.Elements("li").Select(node => new Descriptor
                    {
                        Title = CleanText(node.InnerText),
                        Id = RetriveId(node.Element("a").GetAttributeValue("href", ""))
                    });
                    return vols;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public static async Task<bool> AddUserFavoriteVolume(string volId)
        {
            return false;
            //var b = "/main/add_favourite.html", a = $(this).attr("vol_id");
            //$.post(b, { vol_id: a }, function (c) {
            //	alert(c.msg);
            //}, "json");
            using (var client = NewUserHttpClient())
            {
                var content = new HttpFormUrlEncodedContent(new[]{
                    new KeyValuePair<string, string>("vol_id", volId)
                });
                var resp = await client.PostAsync(new Uri(UserAddFavoritePath), content);
                resp.EnsureSuccessStatusCode();
                var obj = JObject.Parse(await resp.Content.ReadAsStringAsync());
                if (obj.Value<int>("code") == 0)
                    return true;
                else if (obj.Value<int>("code") == 1)
                    return true;
                else
                    return false;
            }
        }

        public static async Task DeleteUserFavorite(string favId)
        {
            return;


            using (var client = NewUserHttpClient())
            {
                UriBuilder builder = new UriBuilder(UserDeleteFavoritePath);
                builder.Query = "fav_id=" + favId;
                try
                {
                    var resp = await client.PostAsync(builder.Uri, new HttpStringContent(""));
                    resp.EnsureSuccessStatusCode();
                    var obj = JObject.Parse(await resp.Content.ReadAsStringAsync());
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        public static async Task DeleteUserFavorite(string[] favIds)
        {
            return;

            using (var client = NewUserHttpClient())
            {
                var content = new HttpFormUrlEncodedContent(favIds.Select(id => new KeyValuePair<string, string>("fav_id[]", id)));
                try
                {
                    var resp = await client.PostAsync(UserDeleteFavoriteBatchUri, content);
                    resp.EnsureSuccessStatusCode();
                    var obj = JObject.Parse(await resp.Content.ReadAsStringAsync());
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public static async Task<IEnumerable<FavourVolume>> GetUserFavoriteVolumesAsync()
        {
            //var header = _cookieContainer.GetCookieHeader(new Uri(SeverBaseUri + UserRecentPath));
            return new List<FavourVolume>();

            using (var client = NewUserHttpClient())
            {
                try
                {
                    //client.DefaultRequestHeaders.Remove("User-Agent");
                    var resp = await client.GetAsync(UserFavourUri);
                    resp.EnsureSuccessStatusCode();
                    var page = await resp.Content.ReadAsStringAsync();
                    //client.DefaultRequestHeaders.Remove("User-Agent");
                    var stream = await client.GetInputStreamAsync(UserGetFavourUri);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.Load(stream.AsStreamForRead());
                    if (htmlDoc.DocumentNode.InnerHtml == "")
                        throw new NotSignedInException();
                    var volNodes = htmlDoc.DocumentNode.Elements("tr").Where(node => node.HasClass("ft-12"));
                    var vols = volNodes.Select(node =>
                    {
                        var vol = new FavourVolume();
                        var td = node.Element("td");
                        vol.FavId = td.Element("input").GetAttributeValue("value", "");
                        td = td.NextSiblingElement("td");
                        vol.SeriesTitle = WebUtility.HtmlDecode(CleanText(td.InnerText));
                        vol.VolumeId = RetriveId(td.Element("a").GetAttributeValue("href", ""));
                        td = td.NextSiblingElement("td");
                        vol.VolumeNo = WebUtility.HtmlDecode(CleanText(td.InnerText));
                        td = td.NextSiblingElement("td");
                        vol.VolumeTitle = WebUtility.HtmlDecode(CleanText(td.InnerText));
                        td = td.NextSiblingElement("td");
                        vol.FavTime = DateTime.Parse(td.InnerText);
                        return vol;
                    });
                    return vols;
                }
                catch (Exception exception)
                {
                    throw exception;
                }
            }
            //return null;
        }

        public static async Task<IDictionary<int, IEnumerable<Comment>>> GetCommentsAsync(string chapterId, string seriesId, string lineId = "*")
        {
            using (var client = NewUserHttpClient())
            {
                try
                {
                    // book, chapter, line
                    var uri = new Uri(String.Format(CommentListPath, seriesId, chapterId, lineId));
                    var response = await client.GetStringAsync(uri);
                    var parsed = JsonObject.Parse(response);

                    //var parsed = JObject.Load(response);
                    var comments = parsed.GetNamedObject("data").GetNamedValue("lists");

                    // empty for this case
                    if (comments.ValueType != JsonValueType.Object)
                        return null;

                    var reuslt = comments.GetObject().ToDictionary(pair => int.Parse(pair.Key), pair => pair.Value.GetArray().Select(token => new Comment
                    {
                        //_id = (uint)(token.GetObject().GetNamedNumber("_id")),
                        cn = token.GetObject().GetNamedString("cn"),
                        //t = (long)(token.GetObject().GetNamedNumber("t"))
                    }));
                    return reuslt;
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("Failed to load comment list : chapter_id = " + chapterId);
                    Debug.WriteLine(exception.Message);
                    //throw exception;
                    return null;
                }
            }
            //}
            //else
            //{
            //    var content = new HttpFormUrlEncodedContent(new[]
            //    {
            //        new KeyValuePair<string, string>("chapter_id",chapterId),
            //    });
            //    using (var client = NewUserHttpClient())
            //    {
            //        try
            //        {
            //            var response = await client.PostAsync(CommentListUri, content);
            //            response.EnsureSuccessStatusCode();
            //            var respStr = await response.Content.ReadAsStringAsync();
            //            var resp = JArray.Parse(respStr);
            //            return resp.Select(obj => int.Parse(obj.ToString()));

            //        }
            //        catch (Exception exception)
            //        {
            //            Debug.WriteLine("Failed to load comment list : chapter_id = " + chapterId);
            //            Debug.WriteLine(exception.Message);
            //            //throw exception;
            //            return new int[] { };
            //        }
            //    }
            //}
        }

        //public static async Task<IEnumerable<string>> GetCommentsAsync(string lineId, string chapterId)
        //{
        //    string CommentGetPath = null;
        //    //if (String.IsNullOrEmpty(AccountUserAgent))
        //    //{
        //    var content = new System.Net.Http.FormUrlEncodedContent(new[]
        //    {
        //            //new KeyValuePair<string, string>("chapter_id",chapterId),
        //            new KeyValuePair<string, string>("line_id",lineId),
        //        });
        //    using (var client = NewDefaultHttpClient())
        //    {
        //        try
        //        {
        //            //http://www.linovel.com/n/get_comment_content/4516	HTTP	POST
        //            Uri uri = new Uri (CommentGetPath + chapterId);
        //            var response = await client.PostAsync(uri, content);
        //            response.EnsureSuccessStatusCode();
        //            var respStr = await response.Content.ReadAsStringAsync();

        //            var resp = JObject.Parse(respStr);
        //            return resp["data"].Values<string>();
        //        }
        //        catch (Exception exception)
        //        {
        //            Debug.WriteLine("Failed to load comment  : line_id = " + lineId);
        //            Debug.WriteLine(exception.Message);
        //            //throw exception;
        //            return null;
        //        }
        //    }
        //    //}
        //    //else
        //    //{
        //    //    var content = new HttpFormUrlEncodedContent(new[]
        //    //    {
        //    //        new KeyValuePair<string, string>("chapter_id",chapterId),
        //    //        new KeyValuePair<string, string>("line_id",lineId),
        //    //    });
        //    //    try
        //    //    {
        //    //        using (var client = NewUserHttpClient())
        //    //        using (var response = await client.PostAsync(CommentGetUri, content))
        //    //        {
        //    //            response.EnsureSuccessStatusCode();
        //    //            var respStr = await response.Content.ReadAsStringAsync();
        //    //            var resp = JArray.Parse(respStr);
        //    //            return resp.Select(obj => obj.ToString()).Reverse();
        //    //        }
        //    //    }
        //    //    catch (Exception exception)
        //    //    {
        //    //        return null;
        //    //    }
        //    //}
        //}

        public static async Task<bool> CreateCommentAsync(string lineId, string chapterId, string content)
        {
            var postContent = new HttpFormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("chapter_id",chapterId),
                new KeyValuePair<string, string>("line_id",lineId),
                new KeyValuePair<string, string>("content",content),
            });
            //var param = "{" + string.Format("chapter_id: {0},line_id: {1},content: {2}", chapterId, lineId, content) + "}";
            using (var client = NewUserHttpClient())
            using (var response = await client.PostAsync(CommentSetUri, postContent))
            {
                if (!response.IsSuccessStatusCode)
                    return false;
                response.EnsureSuccessStatusCode();
                var respStr = await response.Content.ReadAsStringAsync();
                var resp = JObject.Parse(respStr);
                return (string)resp["code"] == "0";
            }
        }

        //public static Chapter GetChapter(string id);
        public async static Task<Volume> GetVolumeAsync(string id, bool forceRefresh = false)
        {
            string VolumeSource = null;
            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            var novelUrl = new Uri(VolumeSource + id + ".html");
            var doc = await GetHtmlDocumentAsync(novelUrl);
            if (doc == null) return null;

            var nodes = doc.DocumentNode.Descendants();

            var chapterListNode = nodes.First(node => node.Name == "li" && node.HasClass("linovel-book-item"));

            var volume = ParseBookItemNode(chapterListNode);
            volume.Id = id;

            var moreNode = nodes.FirstOrDefault(node => node.Name == "a" && node.HasClass("linovel-detail-more"));
            volume.ParentSeriesId = RetriveId(moreNode.GetAttributeValue("href", ""));

            var infoNodes = nodes.FirstOrDefault(node => node.HasClass("linovel-info"))?.Descendants();

            if (infoNodes == null)
                throw new Exception("Parsing Error");
            volume.Title = CleanText(infoNodes.First(
                node => node.Name == "h1"
                ).FirstChild.InnerText);

            // Detail Properties
            var authorNode = infoNodes.First(node => node.Name == "label" && node.InnerText.StartsWith("作者"));
            volume.Author = authorNode.NextSiblingElement().InnerText;
            var illustraotrNode = infoNodes.First(node => node.Name == "label" && node.InnerText.StartsWith("插画"));
            volume.Illustrator = illustraotrNode.NextSiblingElement().InnerText;
            var publisherNode = infoNodes.First(node => node.Name == "label" && node.InnerText.StartsWith("文库"));
            volume.Publisher = publisherNode.NextSiblingElement().InnerText;
            //series.Illustrator = details.First(node => node.Name == "td" && node.InnerText.StartsWith("插画")).NextSibling.InnerText;
            //series.Publisher = details.First(node => node.Name == "td" && node.InnerText.StartsWith("文库")).NextSibling.InnerText;
            //volume.UpdateTime.

            // Description
            var descriptNode = infoNodes.FirstOrDefault(node => node.HasClass("linovel-info-desc"));
            if (!String.IsNullOrEmpty(descriptNode.InnerText))
                volume.Description = WebUtility.HtmlDecode(descriptNode.InnerText);
            else // If the description have some invalid characters... = =|||
            {
                StringBuilder builder = new StringBuilder(200, 400);
                while (descriptNode != null)
                {
                    builder.Append(descriptNode.InnerText);
                    descriptNode = descriptNode.NextSibling;
                }
                volume.Description = builder.ToString();
            }

            var CoverNodes = nodes.Where(node => node.HasClass("linovel-cover"));
            foreach (var coverNode in CoverNodes)
            {
                volume.CoverImageUri = AbsoluteUrl(coverNode.Descendants("img").First().Attributes["src"].Value);
                if (volume.CoverImageUri.Contains("cover/image"))
                    break;
            }

            return volume;
        }

        public static Chapter ParseChapterAlter(string id, HtmlDocument doc)
        {

            var chapter = new Chapter();

            var nodes = doc.DocumentNode.Descendants();
            chapter.Id = id;
            // Navigation Properties
            {
                var navi = nodes.First(
                    node => node.Attributes["class"] != null && node.Attributes["class"].Value.StartsWith("lk-m-view-pager"));
                var naviNodes = navi.Descendants("a");

                var prev = naviNodes.FirstOrDefault(node => node.InnerText.Contains("上一章"));
                if (prev != null)
                    chapter.PrevChapterId = RetriveId(prev.Attributes["href"].Value);
                var content = naviNodes.FirstOrDefault(node => node.Attributes["href"].Value.Contains("book/"));
                if (content != null)
                    chapter.ParentVolumeId = RetriveId(content.Attributes["href"].Value);
                var next = naviNodes.FirstOrDefault(node => node.InnerText.Contains("下一章"));
                if (next != null)
                    chapter.NextChapterId = RetriveId(next.Attributes["href"].Value);
            }

            var canvas = nodes.First(
                node => node.Attributes["class"] != null && node.Attributes["class"].Value.StartsWith("lk-m-view-can"));
            var contents = canvas.ChildNodes;
            var chapterTitleNode = contents.First(node => node.Name == "h3");
            if (chapterTitleNode != null)
                chapter.Title = RemoveLabel(chapterTitleNode.InnerText);

            var lines = from line in contents
                        where line.Name == "div"
                        && line.Attributes["class"] != null
                        && line.Attributes["class"].Value.StartsWith("lk-view-line")
                        select line;

            chapter.Lines = (lines.Select<HtmlNode, Line>(node =>
            {
                Line line = new Line { No= int.Parse(node.Id),Content= null };
                if (!node.HasChildNodes)
                {
                    line.Content = String.Empty;
                    return line;
                }
                if (node.ChildNodes.Any(elem => elem.Name == "div"))
                {
                    var img_url = (from elem in node.Descendants()
                                   where elem.Name == "img"
                                         && elem.Attributes["data-ks-lazyload"] != null
                                   select elem.Attributes["data-ks-lazyload"].Value).FirstOrDefault();
                    if (img_url != null)
                    {
                        line.Content = AbsoluteUrl(img_url);
                    }
                }
                else
                {
                    var text = node.InnerText;
                    line.Content = WebUtility.HtmlDecode(text.Trim());
                }
                return line;
            })).ToList();
            return chapter;
        }

        //public static Volume GetVolume(string id);
        public async static Task<Chapter> GetChapterAsync(string chpId, string volId, string serId)
        {

            var chapter = new Chapter();
            chapter.Id = chpId;
            chapter.ParentVolumeId = volId;
            chapter.ParentSeriesId = serId;
            var novelUrl = new Uri(String.Format(ChapterSource, chpId, volId, serId));
            var doc = await GetHtmlDocumentAsync(novelUrl);
            if (doc == null) return null;

            try
            {
                var article = doc.DocumentNode.Descendants("article").FirstOrDefault();
                var titles = article.PreviousSiblingElement("div");
                chapter.Title = titles?.Element("h1")?.InnerText;
                var lines = article.ChildNodes.Where(node => node.Name == "p" && node.HasClass("read-line"));
                chapter.Lines = lines.Select(
                    (p) =>
                    {
                        var line = new Line
                        {
                            No = int.Parse(p.Attributes["data-i"].Value),
                            Content = string.Empty
                        };
                        if (p.HasClass("read-line-image"))
                        {
                            var img = p.ChildNodes.FirstOrDefault(child => child.Name == "img");
                            line.Content = img.GetAttributeValue("src", string.Empty);
                            line.Width = img.GetAttributeValue("data-width", 0);
                            line.Height = img.GetAttributeValue("data-height", 0);
                            line.Size = ulong.Parse(img.GetAttributeValue("data-size", string.Empty));
                        }
                        else
                        {
                            line.Content = CleanText(WebUtility.HtmlDecode(p.InnerText));
                        }
                        return line;
                    }).ToList();
            }
            catch (Exception excp)
            {
                throw new Exception("Failed to parse content : " + excp.Message, excp);
            }

            return chapter;
        }

        public async static Task<Chapter> GetChapterAlterAsync(string id)
        {
            string ChapterSource1 = null;
            var novelUrl = new Uri(ChapterSource1 + id + ".html" + (UseSimplifiedCharset ? String.Empty : CharsetAffix));
            var doc = await GetHtmlDocumentAsync(novelUrl);
            if (doc == null) return null;
            return ParseChapterAlter(id, doc);
        }

        static NovelPositionIdentifier ParseIDFromReadLink(string link)
        {
            var pos = new NovelPositionIdentifier();
            Uri uri = new Uri(SeverBaseUri, link);
            var decoder = new WwwFormUrlDecoder(uri.Query);
            pos.ChapterId = decoder.GetFirstValueByName("chapterId");
            pos.VolumeId = decoder.GetFirstValueByName("volId");
            pos.SeriesId = decoder.GetFirstValueByName("bookId");
            return pos;
        }

        // return series ID
        static string ParseIDFromBookLink(string link)
        {
            Uri uri = new Uri(SeverBaseUri, link);
            var decoder = new WwwFormUrlDecoder(uri.Query);
            return decoder.GetFirstValueByName("id");
        }

        [Deprecated("Work not", DeprecationType.Deprecate, 7)]
        static string RetriveId(string id)
        {
            return null;
        }

        static Volume ParseVolumeSection(HtmlNode section)
        {
            Volume volume = new Volume();
            volume.CoverImageUri = section.FirstChildClass("cover").Element("img").GetAttributeValue("src", string.Empty);
            volume.Title = WebUtility.HtmlDecode(section.FirstChildClass("info").InnerText);
            volume.Label = ExtractLabel(volume.Title);
            volume.Title = RemoveLabel(volume.Title);
            volume.Chapters = section.Element("ul").Elements("li").Select(li =>
            {
                var cpt = new ChapterProperties();
                var a = li.Element("a");
                var pos = ParseIDFromReadLink(a.GetAttributeValue("href", String.Empty));
                cpt.Id = pos.ChapterId;
                cpt.ParentVolumeId = pos.VolumeId;
                cpt.ParentSeriesId = pos.SeriesId;
                cpt.Title = a.InnerText;
                return cpt;
            }).ToList();

            if (volume.Chapters.Count > 0)
            {
                volume.Id = volume.Chapters[0].ParentVolumeId;
                volume.ParentSeriesId = volume.Chapters[0].ParentSeriesId;
            }

            for (int chapterIdx = 0; chapterIdx < volume.Chapters.Count; chapterIdx++)
            {
                var chapter = volume.Chapters[chapterIdx];
                chapter.ChapterNo = chapterIdx;
                //chapter.ParentVolumeId = vol.Id;
                chapter.ParentVolumeId = volume.Id;
                if (chapterIdx > 0)
                {
                    //chapter.PrevChapter = vol.Chapters[chapterIdx - 1];
                    chapter.PrevChapterId = volume.Chapters[chapterIdx - 1].Id;
                }
                if (chapterIdx < volume.Chapters.Count - 1)
                {
                    //chapter.NextChapter = vol.Chapters[chapterIdx + 1];
                    chapter.NextChapterId = volume.Chapters[chapterIdx + 1].Id;
                }
            }
            return volume;
        }

        //public static Series GetSeries(string id);
        public async static Task<Series> GetSeriesAsync(string id)
        {
            var series = new Series();
            series.Id = id;
            var novelUrl = new Uri(string.Format(SeriesSource, id));
            var doc = await GetHtmlDocumentAsync(novelUrl);
            if (doc == null) return null;

            var nodes = doc.DocumentNode.Descendants();
            var layouts = nodes.FirstOrDefault(node => node.HasClass("layout"));
            var cover = layouts.FirstChildClass("cover");
            var infos = layouts.FirstChildClass("info");
            if (infos == null)
                throw new Exception("Parsing Error : info section not found.");

            series.CoverImageUri = cover.Element("img").GetAttributeValue("src", string.Empty);
            series.Title = CleanText(infos.Descendants("h2").First().FirstChild.InnerText);

            // Detail Properties
            series.Author = infos.Descendants("a").First(node => node.HasClass("author")).InnerText;
            series.Illustrator = infos.Descendants("a").First(node => node.HasClass("artist")).InnerText;
            series.Publisher = infos.Descendants("a").First(node => node.HasClass("publisher")).InnerText;
            //series.Illustrator = details.First(node => node.Name == "td" && node.InnerText.StartsWith("插画")).NextSibling.InnerText;
            //series.Publisher = details.First(node => node.Name == "td" && node.InnerText.StartsWith("文库")).NextSibling.InnerText;
            //volume.UpdateTime.

            // Description
            var intro = infos.Descendants("p").First(node => node.HasClass("intro"));
            series.Description = ParseDescription(intro);

            var volumeListNodes = layouts.FirstChildClass("volume-list").FirstChildClass("list").Elements("section");

            try
            {
                var vols = volumeListNodes.Select(volNode =>
                {
                    return ParseVolumeSection(volNode);
                });

                series.Volumes = vols.ToList();

                for (int volIdx = 0; volIdx < series.Volumes.Count; volIdx++)
                {
                    var vol = series.Volumes[volIdx];
                    vol.VolumeNo = volIdx;
                    vol.ParentSeriesId = series.Id;

                    vol.Author = series.Author;
                    vol.Illustrator = series.Illustrator;
                    vol.Publisher = series.Publisher;

                    if (volIdx > 0)
                    {
                        //vol.PrevVolume = series.Volumes[volIdx-1];
                        vol.PrevVolumeId = series.Volumes[volIdx - 1].Id;
                    }
                    if (volIdx < series.Volumes.Count - 1)
                    {
                        //vol.NextVolume = series.Volumes[volIdx + 1];
                        vol.NextVolumeId = series.Volumes[volIdx + 1].Id;
                    }
                }
            }
            catch (Exception excp)
            {
                throw new Exception("Parsing error : error when parsing volume sections" + excp.Message, excp);
            }

            //series.Volumes = (vols.OrderBy(vol =>
            //{
            //    double l;
            //    if (double.TryParse(vol.Label, out l))
            //        return l;
            //    else
            //    {
            //        int d;
            //        int.TryParse(vol.Id, out d);
            //        return d * 100;
            //    }
            //}
            //    )).ToList();

            return series;
        }

        private static string ParseDescription(HtmlNode intro)
        {
            if (!string.IsNullOrEmpty(intro.InnerText))
            {
                intro = intro?.Element("span");
                intro = intro?.NextSibling;
                return CleanText(WebUtility.HtmlDecode(intro.InnerText));
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                intro = intro?.NextSiblingElement("span")?.NextSibling;
                while (intro != null && intro.Name != "p")
                {
                    if (intro.NextSibling == null & intro.FirstChild != null && intro.FirstChild.NextSibling != null)
                        intro = intro.FirstChild;
                    else
                    {
                        builder.Append(WebUtility.HtmlDecode(intro.InnerText));
                        intro = intro.NextSibling;
                    }
                }
                return CleanText(WebUtility.HtmlDecode(builder.ToString()));
            }
        }

        [Deprecated("This method are not valiadate anymore", DeprecationType.Deprecate, 7)]
        private static Volume ParseBookItemNode(HtmlNode volNode)
        {
            var vol = new Volume();
            vol.Id = RetriveId(volNode.Descendants("a").First().Attributes["href"].Value);

            vol.Title = WebUtility.HtmlDecode(volNode.ChildNodes.FindFirst("h3").InnerText);

            vol.Label = ExtractLabel(vol.Title);
            vol.Title = RemoveLabel(vol.Title);

            vol.CoverImageUri = AbsoluteUrl(volNode.Descendants("img").First().Attributes["src"].Value);

            vol.Chapters = (from chaptersNode in volNode.ChildNodes.First(node => node.HasClass("linovel-chapter-list")).Descendants("a")
                            select new ChapterProperties
                            {
                                Id = RetriveId(chaptersNode.Attributes["href"].Value),
                                Title = RemoveLabel(WebUtility.HtmlDecode(chaptersNode.InnerText)),
                            }).ToList();
            return vol;
        }

        private static string CleanText(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            p = Regex.Replace(p, "[\r\n\t]+", " ");
            p = Regex.Replace(p, "&(.+?);", "");
            p = p.Trim();
            return p;
        }

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
            if (s.StartsWith("第"))
                return s.Substring(1, s.Length - 2);
            else
                return s;
        }

        private static string AbsoluteUrl(string url)
        {
            var uri = new Uri(SeverBaseUri, url);
            return uri.AbsoluteUri;
        }

        private static ExtendedBookItem ParseSearchItem(HtmlNode li)
        {
            ExtendedBookItem book = new ExtendedBookItem();
            try
            {
                var cover = li.FirstChildClass("cover");
                book.CoverImageUri = cover.Element("img").GetAttributeValue("src", null);
                book.SeriesId = ParseIDFromBookLink(cover.GetAttributeValue("href", null));
                var info = li.FirstChildClass("info");
                book.Title = CleanText(WebUtility.HtmlDecode(info.FirstChildClass("title").InnerText));
                var desc = info.FirstChildClass("desc");
                book.Author = desc.Descendants("a").First(node => node.HasClass("author")).InnerText;
                book.Illustrator = desc.Descendants("a").First(node => node.HasClass("artist")).InnerText;
                book.Publisher = desc.Descendants("a").First(node => node.HasClass("publisher")).InnerText;
                var intro = desc.FirstChildClass("intro");
                book.Description = ParseDescription(intro);
            }
            catch (Exception excp)
            {
                throw new Exception("Parsing Error in search result item : " + excp.Message, excp);
            }
            return book;
        }

        public static async Task<List<ExtendedBookItem>> SearchBookAsync(string keyword)
        {
            var url = new Uri(String.Format(QueryPath, WebUtility.HtmlEncode(keyword)));
            var doc = await GetHtmlDocumentAsync(url);
            if (doc == null) return null;

            var result = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.HasClass("search-result"));
            result = result?.Descendants("ul").FirstOrDefault();
            var items = result?.Elements("li");
            if (items != null)
                return items.Select(li => ParseSearchItem(li)).ToList();
            return null;
        }

        //private static BookItem ParseSearchItem(HtmlNode li)
        //{
        //    var book = new BookItem();
        //    book.HyperLinkUri = li.Element("a").GetAttributeValue("href", null);
        //    book.CoverImageUri = AbsoluteUrl(
        //                                 li.Descendants("img").FirstOrDefault()?.GetAttributeValue("data-ks-lazyload", null));
        //    book.Title = li.Element("a").Descendants("h1").FirstOrDefault()?.InnerText;
        //    //book.Description = li.Element("a").Descendants("p").FirstOrDefault()?.InnerText;

        //    //book.VolumeId = RetriveId(book.HyperLinkUri);
        //    var idx = book.Title.IndexOf(' ');
        //    book.Subtitle = book.Title.Substring(idx + 1);
        //    book.Title = book.Title.Substring(0, idx + 1);
        //    book.Id = book.VolumeId;
        //    return book;
        //}
        //public static async Task<List<BookItem>> SearchBookAsync(string keyword)
        //{
        //    var url = new Uri(String.Format(QueryPath, WebUtility.HtmlEncode(keyword)));
        //    var doc = await GetHtmlDocumentAsync(url);
        //    if (doc == null) return null;
        //    //node => node.PreviousSublingElement("strong").InnerText.Contains("卷浏览记录")
        //    var books = (from li in doc.DocumentNode.Descendants("li")
        //                 where
        //                   li.Element("a")?.GetAttributeValue("href", null)?.Contains("book/") == true
        //                 select ParseSearchItem(li)).ToList();

        //    return books;
        //}

        public async static Task<List<Descriptor>> GetSeriesIndexAsync(bool forceRefresh = false)
        {
            var doc = await GetHtmlDocumentAsync(SeriesIndexSourceUri);
            if (doc == null) return null;
            var nodes = doc.DocumentNode.Descendants("a");
            return (from link in nodes
                    where
                        link.Attributes["href"] != null &&
                        link.Attributes["href"].Value.Contains("/book/detail?id=")
                    select new Descriptor
                    {
                        Id = ParseIDFromBookLink(link.Attributes["href"].Value),
                        Title = CleanText(link.InnerText)
                    }).ToList();
        }

        private static ExtendedBookItem ParseExtentedBookItem(JsonObject j)
        {
            var book = new ExtendedBookItem();
            book.SeriesId = ((int)j.GetNamedNumber("_id")).ToString();
            book.Title = j.GetNamedString("name");
            book.CoverImageUri = j.GetNamedString("cover");
            book.Publisher = j.GetNamedString("publisher");
            book.Catalogs = j.GetNamedArray("cat").Select(value => ((BookCatalog)value.GetNumber())).ToList();
            book.Volumes = j.GetNamedArray("vols").Select((v) =>
            {
                var val = v.GetObject();
                string key = val.GetNamedString("_id");
                var value = val.GetNamedArray("chaps").Select(c => c.GetObject().GetNamedString("_id")).ToList();
                var pair = new KeyValuePair<string, List<string>>(key, value);
                return pair;
            }).ToList();
            return book;
        }

        private static IEnumerable<ExtendedBookItem> ParseExtendedBookItems(string content)
        {
            var parsed = JsonObject.Parse(content);
            var list = parsed.GetNamedObject("data").GetNamedArray("lists");
            return list.Select(j => ParseExtentedBookItem(j.GetObject()));
        }

        public async static Task<IEnumerable<ExtendedBookItem>> GetBookListByCatalogAsync(BookCatalog catalog, int page = 1)
        {
            using (var client = NewUserHttpClient())
            {
                var uri = new Uri(String.Format(CatalogPath, (int)catalog, page));
                var response = await client.GetStringAsync(uri);
                return ParseExtendedBookItems(response);
            }
        }

        public async static Task<IEnumerable<ExtendedBookItem>> GetLastestUpdatedAsync()
        {
            using (var client = NewUserHttpClient())
            {
                var response = await client.GetStringAsync(LastesUpdatedUri);
                return ParseExtendedBookItems(response);
            }
        }

        public async static Task<IEnumerable<ExtendedBookItem>> GetNewPublishedAsync()
        {
            using (var client = NewUserHttpClient())
            {
                var response = await client.GetStringAsync(RecentPublishedUri);
                return ParseExtendedBookItems(response);
            }
        }

        public async static Task<IEnumerable<ExtendedBookItem>> GetRecommandedAsync()
        {
            using (var client = NewUserHttpClient())
            {
                var response = await client.GetStringAsync(RecommandUri);
                return ParseExtendedBookItems(response);
            }
        }

        private static IEnumerable<BookItem> ParseBookItemsBlock(HtmlNode block)
        {
            return block.Descendants("a").
                Where(a =>
                    a.Attributes.Contains("href") &&
                    a.GetAttributeValue("href", null).StartsWith("/book/detail")).
                Select(a => ParseBookItem(a));
        }

        private static IEnumerable<BookItem> ParseFeaturedBookItems(HtmlNode indexPage)
        {
            var featured = indexPage.Descendants("div").Where(n => n.HasClass("featured")).FirstOrDefault();
            return ParseBookItemsBlock(featured);
        }

        private static IEnumerable<BookItem> ParseRecomendBookItems(HtmlNode indexPage)
        {
            var featured = indexPage.Descendants("section").Where(n => n.HasClass("recommend")).FirstOrDefault();
            return ParseBookItemsBlock(featured);
        }

        private static IEnumerable<BookItem> ParseRecentUpdateBookItems(HtmlNode indexPage)
        {
            var featured = indexPage.Descendants("section").Where(n => n.HasClass("recent")).FirstOrDefault();
            return ParseBookItemsBlock(featured);
        }

        public async static Task<IDictionary<string, IList<BookItem>>> GetFeaturedBooks()
        {
            var doc = await GetHtmlDocumentAsync(HomePageSourceUri);
            if (doc == null) return null;

            var topBar = doc.DocumentNode.Descendants("div").FirstOrDefault(n => n.HasClass("top-bar"));
            var container = topBar.NextSiblingElement("div");

            Dictionary<string, IList<BookItem>> bookLists = new Dictionary<string, IList<BookItem>>();

            var featured = ParseFeaturedBookItems(container).ToList();
            if (featured.Count > 0)
                bookLists.Add("Featured", featured);
            container = container.Descendants("div").FirstOrDefault(n => n.HasClass("body"));

            var recommend = ParseRecomendBookItems(container).ToList();
            if (recommend.Count > 0)
                bookLists.Add("Recommend", recommend);

            var recent = ParseRecentUpdateBookItems(container).ToList();
            if (recent.Count > 0)
                bookLists.Add("Recent", recent);

            return bookLists;

            var ranks = container.Descendants().Where(x => x.Name == "ul" && x.HasClass("rank"));

            int rankIdx = 0;
            string[] rankNames = new string[] { "Daily Rank", "Weekly Rank", "Mothly Rank" };
            foreach (var ranker in ranks)
            {
                var titleNode = ranker.PreviousSiblingElement("h4");
                var booksNodes = ranker.Descendants("a");
                if (booksNodes != null)
                {
                    var header = CleanText(titleNode?.InnerText);
                    if (string.IsNullOrEmpty(header) && rankIdx <= 2)
                    {
                        header = rankNames[rankIdx++];
                    }

                    var books = (from bookNode in booksNodes
                                 select ParseBookItem(bookNode)).Where(book => !BlackList.Contains(book.Id)).ToList();
                    bookLists.Add(header, books);
                }
            }
            return bookLists;

        }

        // a href = "/book/detail?id=*"
        private static BookItem ParseBookItem(HtmlNode a)
        {
            var book = new BookItem();
            book.HyperLinkUri = a.GetAttributeValue("href", null);
            book.CoverImageUri = a.Element("img")?.GetAttributeValue("src", null);
            book.Title = a.FirstChildClass("title")?.InnerText;

            if (book.Title == null)
                book.Title = CleanText(a.InnerText);

            book.Subtitle = a.FirstChildClass("status")?.InnerText;
            book.SeriesId = ParseIDFromBookLink(book.HyperLinkUri);
            return book;
        }

        public static IEnumerable<Series> GetRecommandListAsync()
        {
            throw new NotImplementedException();
        }
        public static IEnumerable<Series> GetClickRankAsync()
        {
            throw new NotImplementedException();
        }
        public static Task<IEnumerable<Series>> GetFavoriateRankAsync()
        {
            throw new NotImplementedException();
        }
        public static Task UpdateFeedCacheAsync()
        {
            throw new NotImplementedException();
        }

        //public static Task<List<BookmarkInfo>> GetBookmarkInfoAsync()
        //{
        //    return GetFromIsolatedStorageAsAsync<List<BookmarkInfo>>("bookmarks.xml");
        //}

        //public static Task<List<BookmarkInfo>> GetHistoryListAsync()
        //{
        //    return GetFromIsolatedStorageAsAsync<List<BookmarkInfo>>("history.xml");

        //}


        //public static Task SetHistoryListAsync(List<BookmarkInfo> historyList)
        //{
        //    //var storage = IsolatedStorageFile.GetUserStoreForApplication();
        //    return Task.Run(() =>
        //    {
        //        //var stream = storage.OpenFile("history.xml", FileMode.OpenOrCreate, FileAccess.Write);
        //        //var writer = new System.Xml.Serialization.XmlSerializer(typeof(List<BookmarkInfo>));
        //        //writer.Serialize(stream, historyList);
        //        //stream.Close();
        //    });
        //}
        //public static Task SetBookmarkListAsync(List<BookmarkInfo> bookmarkList)
        //{
        //    //var storage = IsolatedStorageFile.GetUserStoreForApplication();
        //    return Task.Run(() =>
        //    {
        //        //var stream = storage.OpenFile("bookmark.xml", FileMode.OpenOrCreate, FileAccess.Write);
        //        //var writer = new System.Xml.Serialization.XmlSerializer(typeof(List<BookmarkInfo>));
        //        //writer.Serialize(stream, bookmarkList);
        //        //stream.Close();
        //    });
        //}


    }
}
