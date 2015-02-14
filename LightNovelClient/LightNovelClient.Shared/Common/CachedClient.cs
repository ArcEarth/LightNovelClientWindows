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

		public static bool IsIllustrationCached(string localname)
		{
			return CachedIllustrationSet.Contains(localname);
		}

		public static async Task InitializeCachedSetAsync()
		{
			var localFolder = ApplicationData.Current.LocalFolder;
			CacheFolder = await localFolder.CreateFolderAsync(CacheFolderName, CreationCollisionOption.OpenIfExists);
			var caches = await CacheFolder.GetFilesAsync();
			if (caches != null)
			{
				CachedSeriesSet.UnionWith(from item in caches where item.Name.StartsWith("series-") select item.Name.Substring(7, item.Name.Length - 12));
				CachedChapterSet.UnionWith(from item in caches where item.Name.StartsWith("chapter-") select item.Name.Substring(8, item.Name.Length - 13));
			}
			if (App.Current.IllustrationFolder == null)
				App.Current.IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);
			var illustrations = await App.Current.IllustrationFolder.GetFilesAsync();
			if (illustrations != null)
			{
				CachedIllustrationSet.UnionWith(from item in illustrations select item.Name);
			}
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

		static Task CacheChaptersAsync(CancellationToken c, IProgress<string> progress, IEnumerable<string> chapters, bool cache_images)
		{
			return Task.Run(async () =>
			{
				var downloader = new BackgroundDownloader();

				foreach (var cid in chapters)
				{
					if (c.IsCancellationRequested)
						break;
					if (!CachedChapterSet.Contains(cid))
					{
						if (c.IsCancellationRequested)
							break;

						try
						{
							var chapter = await GetChapterAsync(cid);
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
											using (var client = new HttpClient())
											{
												StorageFile file = await App.Current.IllustrationFolder.CreateFileAsync(localName, CreationCollisionOption.FailIfExists);
												if (c.IsCancellationRequested)
													return;
												var stream = await client.GetInputStreamAsync(new Uri(img));
												if (c.IsCancellationRequested)
													return;
												var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite);
												if (c.IsCancellationRequested)
													return;
												await stream.AsStreamForRead().CopyToAsync(fileStream.AsStreamForWrite());
												//downloader.CreateDownload(new Uri(img), file);
												CachedIllustrationSet.Add(localName);
											}
										}
										catch (Exception exc)
										{
											Debug.WriteLine(exc.Message);
										}
									});
							}
							progress.Report(cid);
						}
						catch (Exception exc)
						{
							Debug.WriteLine(exc.Message);
							return;
						}
					}
					else
					{
						progress.Report(cid);
					}
				}
			}
			);
		}

		public static IAsyncActionWithProgress<string> CacheChaptersAsync(IEnumerable<string> chapters, bool cache_images = false)
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

		public static Task<Chapter> GetChapterAsync(string id, bool forceRefresh = false)
		{
			if (!forceRefresh && ChapterCache.ContainsKey(id) && !ChapterCache[id].IsFaulted)
				return ChapterCache[id];
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

			var task = DataCache.GetAsync("chapter-" + id, () => LightKindomHtmlClient.GetChapterAlterAsync(id), null, forceRefresh);
			ChapterCache[id] = task;
			CachedChapterSet.Add(id);
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

		public static Task<IList<KeyValuePair<string, IList<BookItem>>>> GetRecommandedBookLists()
		{
			return DataCache.GetAsync("popular_series", LightKindomHtmlClient.GetRecommandedBookLists, DateTime.Now.AddDays(1));
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
				if (await App.Current.SignInAutomaticllyAsync(true))
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
