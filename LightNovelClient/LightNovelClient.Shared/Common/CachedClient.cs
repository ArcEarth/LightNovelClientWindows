using LightNovel.Service;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Web;
using Q42.WinRT.Data;
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

namespace LightNovel.Common
{
	public static class CachedClient
	{
		const string CacheFolderName = "_jsoncache";
		static StorageFolder CacheFolder;
		static Queue<IAsyncActionWithProgress<string>> CachingTaskQueue;
		public static HashSet<string> CachedSeriesSet = new HashSet<string>();
		public static HashSet<string> CachedChapterSet = new HashSet<string>();
		public static HashSet<string> CachedIllustrationSet = new HashSet<string>();
		public static Dictionary<string, Task<Chapter>> ChapterCache = new Dictionary<string, Task<Chapter>>();
		public static Dictionary<string, Task<Series>> SeriesCache = new Dictionary<string, Task<Series>>();
		public static Dictionary<string, Task<Volume>> VolumeCache = new Dictionary<string, Task<Volume>>();

		//public static event EventHandler<string> OnCahpterCached
#if WINDOWS_PHONE_APP
		private static int MaxCachedUnit = 10;
#else
		private static int MaxCachedUnit = 50;
#endif
		public static bool IsSeriesMetaCached(string id)
		{
			return CachedSeriesSet.Contains(id);
		}
		public static bool IsChapterCached(string id)
		{
			return CachedChapterSet.Contains(id);
		}
		public static bool IsIllustrationCached(string img_url)
		{
			var localName = System.IO.Path.GetFileName(img_url);
			return CachedIllustrationSet.Contains(localName);
		}

		public static async Task<bool> InitializeCachedSetAsync()
		{
			var localFolder = ApplicationData.Current.LocalFolder;
			try
			{
				CacheFolder = await localFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
				var caches = await CacheFolder.GetFilesAsync();
				if (caches != null)
				{
					CachedSeriesSet.UnionWith(from item in caches where item.Name.StartsWith("series-") select item.Name.Substring(7, item.Name.Length - 12));
					CachedChapterSet.UnionWith(from item in caches where item.Name.StartsWith("chapter-") select item.Name.Substring(8, item.Name.Length - 13));
				}
			}
			catch (Exception)
			{
				return false;
			}

			try
			{
				if (AppGlobal.IllustrationFolder == null)
                    AppGlobal.IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);
				var illustrations = await AppGlobal.IllustrationFolder.GetFilesAsync();
				if (illustrations != null)
				{
					CachedIllustrationSet.UnionWith(from item in illustrations select item.Name);
				}
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		public static Uri GetIllustrationCachedUri(string img_url)
		{
			var localName = System.IO.Path.GetFileName(img_url);
			if (CachedIllustrationSet.Contains(localName))
			{
				return new Uri("ms-appdata:///local/illustration/" + localName);
			}
			else
				return new Uri(img_url);
		}
		public async static Task DeleteIllustationAsync(string img_url)
		{
			var localName = System.IO.Path.GetFileName(img_url);
			CachedIllustrationSet.Remove(img_url);
			try
			{
				var item = await AppGlobal.IllustrationFolder.GetItemAsync(localName);
				await item.DeleteAsync();
			}
			catch (Exception)
			{
			}
		}


		static Task CacheChaptersAsync(CancellationToken c, IProgress<string> progress, IEnumerable<NovelPositionIdentifier> chapters, bool cache_images)
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

		public static IAsyncActionWithProgress<string> CacheChaptersAsync(IEnumerable<NovelPositionIdentifier> chapters, bool cache_images = false)
		{
			//while (CachingTaskQueue.Count > 0 && (CachingTaskQueue.Peek().AsTask().IsCompleted))
			//{
			//	CachingTaskQueue.Dequeue();
			//}
			var action = AsyncInfo.Run<string>((c, ip) => CacheChaptersAsync(c, ip, chapters, cache_images));
			//CachingTaskQueue.Enqueue(action);
			return action;
		}

		public static Task<Series> GetSeriesAsync(string id, bool forceRefresh = false)
		{
			if (forceRefresh == false && SeriesCache.ContainsKey(id) && !SeriesCache[id].IsFaulted)
				return SeriesCache[id];
			if (SeriesCache.Count > MaxCachedUnit)
			{
				var outdates = (from item in SeriesCache where item.Value.IsCompleted select item.Key).ToArray(); ;
				foreach (var key in outdates)
				{
					SeriesCache.Remove(key);
					if (SeriesCache.Count < MaxCachedUnit)
						break;
				}
			}

			var task = DataCache.GetAsync("series-" + id, () => LightKindomHtmlClient.GetSeriesAsync(id), DateTime.Now.AddDays(7), forceRefresh);
			SeriesCache[id] = task;
			CachedSeriesSet.Add(id);
			return task;
		}
		public static Task<Volume> GetVolumeAsync(string id, bool forceRefresh = false)
		{
			if (!forceRefresh && VolumeCache.ContainsKey(id) && !VolumeCache[id].IsFaulted)
				return VolumeCache[id];
			if (VolumeCache.Count > MaxCachedUnit)
			{
				var outdates = (from item in VolumeCache where item.Value.IsCompleted select item.Key).ToArray(); ;
				foreach (var key in outdates)
				{
					VolumeCache.Remove(key);
					if (VolumeCache.Count < MaxCachedUnit)
						break;
				}
			}

			var task = DataCache.GetAsync("volume-" + id, () => LightKindomHtmlClient.GetVolumeAsync(id), null, forceRefresh);
			VolumeCache[id] = task;
			return task;
		}

        public static Task<Chapter> GetChapterAsync(NovelPositionIdentifier pos, bool forceRefresh = false)
        {
            return GetChapterAsync(pos.ChapterId, pos.VolumeId, pos.SeriesId);
        }

        public static Task<Chapter> GetChapterAsync(string chptId,string volId,string serId, bool forceRefresh = false)
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

			var task = DataCache.GetAsync("chapter-" + chptId, () => LightKindomHtmlClient.GetChapterAsync(chptId,volId,serId), null, forceRefresh);
			ChapterCache[chptId] = task;
			CachedChapterSet.Add(chptId);
			return task;
		}

		public static async Task<List<Descriptor>> GetSeriesIndexAsync(bool forceRefresh = false)
		{
			var index = await DataCache.GetAsync("series_index", () => LightKindomHtmlClient.GetSeriesIndexAsync(), DateTime.Now.AddDays(7));
			if (index.Count == 0)
			{
				index = await DataCache.GetAsync("series_index", () => LightKindomHtmlClient.GetSeriesIndexAsync(), DateTime.Now.AddDays(7), true);
			}
			return index;
		}

		public static Task<IDictionary<string, IList<BookItem>>> GetRecommandedBookLists(bool forceRefresh = false)
		{
			return DataCache.GetAsync("popular_series", LightKindomHtmlClient.GetFeaturedBooks, DateTime.Now.AddDays(1), forceRefresh);
		}
		public async static Task<bool> ClearSerialCache(string serId)
		{
			if (!CachedSeriesSet.Contains(serId))
				return false;
			var ser = await GetSeriesAsync(serId);
			foreach (var vol in ser.Volumes)
			{
				foreach (var cpt in vol.Chapters)
				{
					if (CachedChapterSet.Contains(cpt.Id))
					{
						try
						{
							var chapter = await GetChapterAsync(cpt.Id,vol.Id,ser.Id);
							foreach (var imageLine in chapter.Lines.Where(line => line.ContentType == LineContentType.ImageContent))
							{
								await DeleteIllustationAsync(imageLine.Content);
							}
						}
						catch (Exception)
						{
						}

						await DataCache.Delete("chapter-" + cpt.Id);
						CachedSeriesSet.Remove(cpt.Id);
					}
				}
			}
			await DataCache.Delete("series-" + serId);
			CachedSeriesSet.Remove(serId);
			return true;
		}
		public static Task ClearCache()
		{
			return DataCache.ClearAll();
		}

		internal static void UpdateCachedUserFavoriteVolumes(IEnumerable<FavourVolume> fav_list)
		{
			DataCache.Set<IEnumerable<FavourVolume>>("user_fav", fav_list, DateTime.Now.AddDays(1));
		}

		internal static async Task<IEnumerable<FavourVolume>> GetUserFavoriteVolumesAsync(bool foreceRefresh = false)
		{
			bool IsSigninError = false;
			try
			{
				var fav = await DataCache.GetAsync("user_fav", LightKindomHtmlClient.GetUserFavoriteVolumesAsync, DateTime.Now.AddDays(1), foreceRefresh);
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
					return await DataCache.GetAsync("user_fav", LightKindomHtmlClient.GetUserFavoriteVolumesAsync, DateTime.Now.AddDays(1), true);
				}
			}
			return null;
		}

		internal static async Task ClearUserFavoriteCacheAsync()
		{
			await DataCache.Delete("user_fav");
		}
	}
}
