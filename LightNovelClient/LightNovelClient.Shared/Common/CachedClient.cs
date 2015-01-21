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

namespace LightNovel.Common
{
	public static class CachedClient
	{
		public static Dictionary<string, Task<Chapter>> ChapterCache = new Dictionary<string, Task<Chapter>>();
		public static Dictionary<string, Task<Series>> SeriesCache = new Dictionary<string,Task<Series>>();
		public static Dictionary<string, Task<Volume>> VolumeCache = new Dictionary<string, Task<Volume>>();
		private static int MaxCachedUnit = 50;
		public static Task<Series> GetSeriesAsync(string id, bool forceRefresh = false)
		{
			if (forceRefresh == false && SeriesCache.ContainsKey(id) && !SeriesCache[id].IsFaulted)
				return SeriesCache[id];
			if (SeriesCache.Count > MaxCachedUnit)
			{
				var outdates = from item in SeriesCache where item.Value.IsCompleted select item.Key;
				foreach (var key in outdates)
					SeriesCache.Remove(key);
			}

			var task = DataCache.GetAsync("series-" + id, () => LightKindomHtmlClient.GetSeriesAsync(id),DateTime.Now.AddDays(7),forceRefresh);
			SeriesCache[id] = task;
			return task;
		}
		public static Task<Volume> GetVolumeAsync(string id, bool forceRefresh = false)
		{
			if (VolumeCache.ContainsKey(id) && !VolumeCache[id].IsFaulted)
				return VolumeCache[id];
			if (VolumeCache.Count > MaxCachedUnit)
			{
				var outdates = from item in VolumeCache where item.Value.IsCompleted select item.Key;
				foreach (var key in outdates)
					VolumeCache.Remove(key);
			}

			var task = DataCache.GetAsync("volume-" + id, () => LightKindomHtmlClient.GetVolumeAsync(id),null,forceRefresh);
			VolumeCache[id] = task;
			return task;
		}
		public static Task<Chapter> GetChapterAsync(string id, bool forceRefresh = false)
		{
			if (ChapterCache.ContainsKey(id) && !ChapterCache[id].IsFaulted)
				return ChapterCache[id];
			if (ChapterCache.Count > MaxCachedUnit)
			{
				var outdates = from item in ChapterCache where item.Value.IsCompleted select item.Key;
				foreach (var key in outdates)
					ChapterCache.Remove(key);
			}

			var task = DataCache.GetAsync("chapter-" + id, () => LightKindomHtmlClient.GetChapterAsync(id),null,forceRefresh);
			ChapterCache[id] = task;
			return task;
		}

		public static async Task<List<Descriptor>> GetSeriesIndexAsync(bool forceRefresh = false)
		{
			var index = await DataCache.GetAsync("series_index", () => LightKindomHtmlClient.GetSeriesIndexAsync(), DateTime.Now.AddDays(7));
			if (index.Count == 0)
			{
				index = await DataCache.GetAsync("series_index", () => LightKindomHtmlClient.GetSeriesIndexAsync(), DateTime.Now.AddDays(7),true);
			}
			return index;
		}

		public static Task<IList<KeyValuePair<string, IList<BookItem>>>> GetRecommandedBookLists()
		{
			return DataCache.GetAsync("popular_series",LightKindomHtmlClient.GetRecommandedBookLists, DateTime.Now.AddDays(1));
		}

		public static Task ClearCache()
		{
			return DataCache.ClearAll();
		}

		internal static void UpdateCachedUserFavoriteVolumes(IEnumerable<FavourVolume> fav_list)
		{
			DataCache.Set<IEnumerable<FavourVolume>>("user_fav",fav_list,DateTime.Now.AddDays(1));
		}

		internal static Task<IEnumerable<FavourVolume>> GetUserFavoriteVolumesAsync(bool foreceRefresh = false)
		{
			return DataCache.GetAsync("user_fav", LightKindomHtmlClient.GetUserFavoriteVolumesAsync, DateTime.Now.AddDays(1), foreceRefresh);
		}

		internal static async Task ClearUserFavoriteCacheAsync()
		{
			await DataCache.Delete("user_fav");
		}
	}
}
