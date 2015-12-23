using LightNovel.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Web;
using Windows.Web.Http;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Search;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Networking.BackgroundTransfer;
using Windows.UI.Xaml.Media.Imaging;
using WinRTXamlToolkit.AwaitableUI;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace LightNovel.Data
{
    public class CachedClient
    {
        const string CacheFolderName = "books";

        const string IllustrationFolderName = "illustrations";
        const string ChaptersFolderName = "chapters";
        const string CommentsFolderName = "comments";
        const string BookFolderName = "{0}";
        const string SeriesMetaFileName = "index.json";
        const string ChapterFileName = "{0}.json";
        const string ChapterCommentsFileName = "{0}.comments.json";
        const string IllustrationUriTemplate = "ms-appdata:///localcache/books/{0}/illustration/{1}";

        static StorageFolder CacheFolder;
        static Queue<IAsyncActionWithProgress<string>> CachingTaskQueue;

        public static Dictionary<string, CachedClient> ClientsForSeries = new Dictionary<string, CachedClient>();

        public Dictionary<string, Task<Chapter>> ChapterCache = new Dictionary<string, Task<Chapter>>();

        public StorageFolder BookFolder;
        public StorageFolder IllustrationFolder;
        public StorageFolder ChaptersFolder;
        public StorageFolder CommentsFolder;

        public HashSet<string> CachedChapterSet = new HashSet<string>();
        public Dictionary<string, DateTimeOffset> CachedCommentTimes = new Dictionary<string, DateTimeOffset>();
        public HashSet<string> CachedIllustrationSet = new HashSet<string>();

        string _seriesId;
        public string SeriesID { get { return _seriesId; } }
        public Series Index { get; private set; }
        DateTimeOffset _indexUpdatedTime;
        Task<Series> _indexUpdateTask;
        //public static event EventHandler<string> OnCahpterCached
#if WINDOWS_PHONE_APP
		private static int MaxCachedUnit = 10;
#else
        private static int MaxCachedUnit = 50;
#endif
        public static bool IsSeriesMetaCached(string id)
        {
            return ClientsForSeries.ContainsKey(id);
        }

        public bool IsChapterCached(string id)
        {
            return CachedChapterSet.Contains(id);
        }
        public bool IsIllustrationCached(string img_url)
        {
            var localName = System.IO.Path.GetFileName(img_url);
            return CachedIllustrationSet.Contains(localName);
        }

        private async static Task<T> GetItemFromFileAsync<T>(StorageFile file) where T : class
        {
            try
            {
                var content = await Windows.Storage.FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return null;
            }
        }

        private async static Task<bool> SaveItemToFileAsync(StorageFile file, object item)
        {
            try
            {
                var content = JsonConvert.SerializeObject(item);
                await Windows.Storage.FileIO.WriteTextAsync(file, content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static CachedClient GetForSeries(string seriesId)
        {
            if (!ClientsForSeries.ContainsKey(seriesId) || ClientsForSeries[seriesId] == null)
            {
                var client = new CachedClient();
                client._seriesId = seriesId;
                if (ClientsForSeries.ContainsKey(seriesId))
                    ClientsForSeries.Remove(seriesId);
                ClientsForSeries.Add(seriesId, client);
            }
            return ClientsForSeries[seriesId];
        }

        // Get An Index ready cache client
        public static async Task<CachedClient> GetForSeriesAsync(string seriesId, bool forceRefresh = false)
        {
            var client = GetForSeries(seriesId);
            if (client.BookFolder == null)
                await client.ScanAsync();
            if (client.Index == null)
                await client.GetIndexAsync(forceRefresh);
            return client;
        }

        public static async Task<Series> GetSeriesAsync(string seriesId, bool forceRefresh = false)
        {
            var client = await GetForSeriesAsync(seriesId, forceRefresh);
            return client.Index;
        }


        public async Task ScanAsync()
        {
            var seriesFolderName = String.Format(BookFolderName, _seriesId);
            var folderTask = CacheFolder.TryGetItemAsync(seriesFolderName);

            if (folderTask != null)
                BookFolder = await folderTask as StorageFolder;
            bool newCreated = BookFolder == null;

            if (BookFolder == null)
                BookFolder = await CacheFolder.CreateFolderAsync(seriesFolderName, CreationCollisionOption.OpenIfExists);

            if (IllustrationFolder == null)
                IllustrationFolder = await BookFolder.CreateFolderAsync(IllustrationFolderName, CreationCollisionOption.OpenIfExists);
            if (ChaptersFolder == null)
                ChaptersFolder = await BookFolder.CreateFolderAsync(ChaptersFolderName, CreationCollisionOption.OpenIfExists);
            if (CommentsFolder == null)
                CommentsFolder = await BookFolder.CreateFolderAsync(CommentsFolderName, CreationCollisionOption.OpenIfExists);

            if (Index == null)
            {
                var metaTask = BookFolder.TryGetItemAsync(SeriesMetaFileName);
                if (metaTask != null)
                {
                    var item = (await metaTask) as StorageFile;
                    if (item != null)
                    {
                        var props = await item.GetBasicPropertiesAsync();
                        _indexUpdatedTime = props.DateModified;
                    }
                }
            }

            if (!newCreated)
            {
                var chpts = await ChaptersFolder.GetFilesAsync();
                foreach (var item in chpts)
                {
                    var cid = item.Name.Substring(0, item.Name.Length - ".json".Length);
                    CachedChapterSet.Add(cid);
                }

                var illus = await IllustrationFolder.GetFilesAsync();
                foreach (var item in illus)
                {
                    var iid = item.Name;
                    CachedChapterSet.Add(iid);
                }

                var cmts = await CommentsFolder.GetFilesAsync();
                foreach (var item in cmts)
                {
                    var cid = item.Name.Substring(0, item.Name.Length - ".comments.json".Length);
                    var props = await item.GetBasicPropertiesAsync();
                    CachedCommentTimes.Add(cid, props.DateModified);
                }
            }
        }

        public static async Task InitializeCachedSetAsync()
        {
            var localFolder = ApplicationData.Current.LocalCacheFolder;
            CacheFolder = await localFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);

            var folders = await CacheFolder.GetFoldersAsync();
            foreach (var folder in folders)
            {
                int result = -1;
                if (int.TryParse(folder.Name, out result))
                {
                    ClientsForSeries.Add(folder.Name,null);
                }
            }
        }

        private async Task<IRandomAccessStream> LoadIllustrationAsyncInternal(CancellationToken c, IProgress<int> progress, string img_url)
        {
            var localName = System.IO.Path.GetFileName(img_url);
            StorageFile file = await IllustrationFolder.TryGetItemAsync(localName) as StorageFile;
            IRandomAccessStream fileStream = null;
            progress.Report(0);

            if (c.IsCancellationRequested)
                return null;

            if (file != null)
            {
                var prop = await file.GetBasicPropertiesAsync();
                // Image is corrupted
                if (prop.Size <= 10)
                    file = null; 
            }

            if (file == null)
            {
                file = await IllustrationFolder.CreateFileAsync(localName,CreationCollisionOption.ReplaceExisting);

                fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                using (var client = new HttpClient())
                {
                    if (c.IsCancellationRequested)
                        return null;
                    progress.Report(5);
                    var rmuri = new Uri(img_url);
                    var download = client.GetAsync(rmuri);
                    download.Progress = (info, p) =>
                    {
                        double percentage = (double)p.BytesReceived / (double)p.TotalBytesToReceive;
                        progress.Report((int)(percentage * 80));
                        if (c.IsCancellationRequested)
                            info.Cancel();
                    };
                    if (c.IsCancellationRequested)
                        return null;
                    var stream = await (await download).Content.ReadAsInputStreamAsync();
                    progress.Report(85);
                    await stream.AsStreamForRead().CopyToAsync(fileStream.AsStreamForWrite());
                    progress.Report(95);
                    await fileStream.FlushAsync();
                }
            }
            else
            {
                fileStream = await file.OpenAsync(FileAccessMode.Read);
            }

            CachedIllustrationSet.Add(localName);
            progress.Report(100);

            return fileStream;
        }

        public IAsyncOperationWithProgress<IRandomAccessStream,int> GetIllustrationAsync(string img_url)
        {
            return AsyncInfo.Run<IRandomAccessStream, int>((c,p)=> LoadIllustrationAsyncInternal(c,p,img_url));
        }

        public Uri GetIllustrationCachedUri(string img_url)
        {
            var localName = System.IO.Path.GetFileName(img_url);
            if (CachedIllustrationSet.Contains(localName))
            {
                return new Uri(String.Format(IllustrationUriTemplate, _seriesId, localName));
            }
            else
                return new Uri(img_url);
        }

        public async Task DeleteIllustationAsync(string img_url)
        {
            var localName = System.IO.Path.GetFileName(img_url);
            CachedIllustrationSet.Remove(img_url);
            try
            {
                var item = await IllustrationFolder.GetItemAsync(localName);
                await item.DeleteAsync();
            }
            catch (Exception)
            {
            }
        }


        Task CacheChaptersAsync(CancellationToken c, IProgress<string> progress, IEnumerable<NovelPositionIdentifier> chapters, bool cache_images)
        {
            return Task.Run(async () =>
            {
                //var downloader = new BackgroundDownloader();

                foreach (var cid in chapters)
                {
                    if (c.IsCancellationRequested)
                        break;
                    if (!CachedChapterSet.Contains(cid.ChapterId))
                    {
                        if (c.IsCancellationRequested)
                            break;

                        try
                        {
                            var chapter = await GetChapterAsync(cid.ChapterId, cid.VolumeId, cid.SeriesId);
                            if (cache_images)
                            {
                                var images = from line in chapter.Lines where line.ContentType == LineContentType.ImageContent select line.Content;
                                //var bitmaps = (from img in images select (new BitmapImage(new Uri(img)))).ToArray();
                                //Task.WaitAll(bitmaps.Select(bitmap => bitmap.WaitForLoadedAsync()).ToArray());
                                Parallel.ForEach(images, new ParallelOptions { CancellationToken = c, MaxDegreeOfParallelism = 4 }, async img =>
                                    {
                                        if (c.IsCancellationRequested)
                                            return;
                                        // Cache the image
                                        //BitmapImage image = new BitmapImage(new Uri(img));

                                        //await image.WaitForLoadedAsync();

                                        var localName = System.IO.Path.GetFileName(img);
                                        if (CachedIllustrationSet.Contains(localName))
                                            return;

                                        try
                                        {
                                            //using (var client = new HttpClient())
                                            //{
                                            StorageFile file = await AppGlobal.IllustrationFolder.CreateFileAsync(localName, CreationCollisionOption.FailIfExists);
                                            if (c.IsCancellationRequested)
                                                return;


                                            //var stream = await client.GetInputStreamAsync(new Uri(img));
                                            //if (c.IsCancellationRequested)
                                            //	return;
                                            //var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
                                            //if (c.IsCancellationRequested)
                                            //	return;
                                            //await stream.AsStreamForRead().CopyToAsync(fileStream.AsStreamForWrite());

                                            bool succ = false;
                                            try
                                            {
                                                var downloader = new BackgroundDownloader();
                                                var task = downloader.CreateDownload(new Uri(img), file);

                                                if (c.IsCancellationRequested)
                                                    return;

                                                await task.StartAsync();

                                                CachedIllustrationSet.Add(localName);
                                                succ = true;
                                            }
                                            catch (Exception)
                                            {
                                                succ = false;
                                            }

                                            if (!succ)
                                            {
                                                await file.DeleteAsync(); // Remove the file as it may mislead the system
                                            }
                                            //}
                                        }
                                        catch (Exception exc)
                                        {
                                            Debug.WriteLine(exc.Message);
                                        }
                                    });
                            }
                            progress.Report(cid.ChapterId);
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine(exc.Message);
                            return;
                        }
                    }
                    else
                    {
                        progress.Report(cid.ChapterId);
                    }
                }
            }
            );
        }

        public IAsyncActionWithProgress<string> CacheChaptersAsync(IEnumerable<NovelPositionIdentifier> chapters, bool cache_images = false)
        {
            //while (CachingTaskQueue.Count > 0 && (CachingTaskQueue.Peek().AsTask().IsCompleted))
            //{
            //	CachingTaskQueue.Dequeue();
            //}
            var action = AsyncInfo.Run<string>((c, ip) => CacheChaptersAsync(c, ip, chapters, cache_images));
            //CachingTaskQueue.Enqueue(action);
            return action;
        }

        public static async Task<T> TryGetCachedAsync<T>(StorageFolder folder, string fileName) where T : class
        {
            var file = await folder.TryGetItemAsync(fileName) as StorageFile;
            if (file != null)
                return await GetItemFromFileAsync<T>(file);
            return null;
        }

        public static async Task<T> GetAsync<T>(StorageFolder folder, string fileName, Func<Task<T>> generator, Nullable<TimeSpan> expires = null, bool forceRefresh = false) where T : class
        {
            T result = null;
            var file = await folder.TryGetItemAsync(fileName) as StorageFile;
            if (file != null && !forceRefresh)
            {
                var prop = await file.GetBasicPropertiesAsync();

                if (prop.Size > 0 &&
                    (!expires.HasValue || 
                    prop.DateModified + expires.Value > DateTime.Now))
                {
                    result = await GetItemFromFileAsync<T>(file);
                    return result;
                }
            }

            result = await generator.Invoke();

            if (result != null)
            {
                // In fact, we can bypass this wait here
                file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                await SaveItemToFileAsync(file, result);
            }

            return result;
        }

        public async Task<Series> GetIndexAsync(bool forceRefresh = false)
        {
            if (_indexUpdateTask != null)
            {
                return await _indexUpdateTask;
            }

            if (Index == null || forceRefresh)
            {
                _indexUpdateTask = GetAsync(BookFolder, SeriesMetaFileName, () => LightKindomHtmlClient.GetSeriesAsync(_seriesId), TimeSpan.FromDays(7), forceRefresh).ContinueWith(ts => {
                    Index = ts.Result;
                    _indexUpdateTask = null;
                    return ts.Result;
                });
                await _indexUpdateTask;
            }
            return Index;
        }

        //public static Task<Series> GetSeriesAsync(string id, bool forceRefresh = false)
        //{
        //    if (forceRefresh == false && SeriesCache.ContainsKey(id) && !SeriesCache[id].IsFaulted)
        //        return SeriesCache[id];

        //    if (SeriesCache.Count > MaxCachedUnit)
        //    {
        //        var outdates = (from item in SeriesCache where item.Value.IsCompleted select item.Key).ToArray(); ;
        //        foreach (var key in outdates)
        //        {
        //            SeriesCache.Remove(key);
        //            if (SeriesCache.Count < MaxCachedUnit)
        //                break;
        //        }
        //    }

        //    var task = GetAsync("series-" + id, () => LightKindomHtmlClient.GetSeriesAsync(id), DateTime.Now.AddDays(7), forceRefresh);
        //    SeriesCache[id] = task;
        //    CachedSeriesSet.Add(id);
        //    return task;
        //}

        public Task<Chapter> GetChapterAsync(NovelPositionIdentifier pos, bool forceRefresh = false)
        {
            return GetChapterAsync(pos.ChapterId, pos.VolumeId, pos.SeriesId);
        }

        public Task<Chapter> GetChapterAsync(string chptId, string volId, string serId, bool forceRefresh = false)
        {
            if (!forceRefresh && ChapterCache.ContainsKey(chptId) && !ChapterCache[chptId].IsFaulted)
                return ChapterCache[chptId];
            if (ChapterCache.Count > MaxCachedUnit)
            {
                var outdates = (from item in ChapterCache where item.Value.IsCompleted select item.Key).ToArray();

                foreach (var key in outdates)
                {
                    ChapterCache.Remove(key);
                    if (ChapterCache.Count < MaxCachedUnit)
                        break;
                }
            }

            var task = GetAsync<Chapter>(
                ChaptersFolder,
                string.Format(ChapterFileName,chptId), 
                () => LightKindomHtmlClient.GetChapterAsync(chptId, volId, serId),
                null, forceRefresh);

            ChapterCache[chptId] = task;
            CachedChapterSet.Add(chptId);
            return task;
        }

        public static async Task<List<Descriptor>> GetSeriesIndexAsync(bool forceRefresh = false)
        {
            var index = await GetAsync(CacheFolder,"series_index.json", () => LightKindomHtmlClient.GetSeriesIndexAsync(), TimeSpan.FromDays(1));
            if (index.Count == 0)
            {
                index = await GetAsync(CacheFolder, "series_index.json", () => LightKindomHtmlClient.GetSeriesIndexAsync(), TimeSpan.FromDays(1), true);
            }
            return index;
        }

        public static Task<IDictionary<string, IList<BookItem>>> GetRecommandedBookLists(bool forceRefresh = false)
        {
            return GetAsync(CacheFolder, "popular_series.json", LightKindomHtmlClient.GetFeaturedBooks, TimeSpan.FromDays(1), forceRefresh);
        }

        public static async Task DeleteSeries(string serid)
        {
            if (ClientsForSeries.ContainsKey(serid))
            {
                ClientsForSeries.Remove(serid);
            }
            var folder = await CacheFolder.TryGetItemAsync(serid) as StorageFolder;
            await folder.DeleteAsync();
        }

        public static async Task DeleteAllSeries()
        {
            var series = ClientsForSeries.Keys.ToArray();
            foreach (var ser in series)
            {
                await DeleteSeries(ser);
            }
        }

        internal static void UpdateCachedUserFavoriteVolumes(IEnumerable<FavourVolume> fav_list)
        {
            //DataCache.Set<IEnumerable<FavourVolume>>("user_fav", fav_list, DateTime.Now.AddDays(1));
        }

        internal static async Task<IEnumerable<FavourVolume>> GetUserFavoriteVolumesAsync(bool foreceRefresh = false)
        {
            bool IsSigninError = false;
            try
            {
                var fav = await GetAsync(CacheFolder,"user_fav", LightKindomHtmlClient.GetUserFavoriteVolumesAsync, TimeSpan.FromDays(1), foreceRefresh);
                return fav;
            }
            catch (NotSignedInException)
            {
                IsSigninError = true;
            }
            catch (Exception excp)
            {
                throw excp;
            }
            if (IsSigninError)
            {
                if (await AppGlobal.SignInAutomaticllyAsync(true))
                {
                    return await GetAsync(CacheFolder,"user_fav.json", LightKindomHtmlClient.GetUserFavoriteVolumesAsync, TimeSpan.FromDays(1), true);
                }
            }
            return null;
        }

        internal static async Task ClearUserFavoriteCacheAsync()
        {
            //await Delete("user_fav");
        }
    }
}
