using LightNovel.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace LightNovel.Common
{
	public class UserInfo
	{
		public UserInfo()
		{
			IsUserFavoriteValiad = true;
		}
		public string Id { get; set; }
		public string Alias { get; set; }
		public string UserName { get; set; }
		public string EmailAddress { get; set; }
		public string ProfilePicture { get; set; }
		public string Password { get; set; }
		public Session Credential { get; set; }
		public List<BookmarkInfo> RecentList { get; set; }
		public ObservableCollection<FavourVolume> FavoriteList { get; set; }
		public static bool IsUserFavoriteValiad
		{
			get;
			private set;
		}

		class FavItemEqualityComparer : IEqualityComparer<FavourVolume>
		{
			public bool Equals(FavourVolume f1, FavourVolume f2)
			{
				return f1.VolumeId == f2.VolumeId;
			}


			public int GetHashCode(FavourVolume fx)
			{
				return fx.VolumeId.GetHashCode();
			}
		}

		public async Task SyncFavoriteListAsync(bool forceRefresh = false)
		{
			try
			{
				var fav = await CachedClient.GetUserFavoriteVolumesAsync(!IsUserFavoriteValiad || forceRefresh);
				if (FavoriteList == null)
				{
					FavoriteList = new ObservableCollection<FavourVolume>(fav);
				}
				else
				{
					//FavoriteList.Clear();
					var olds = FavoriteList.Except(fav, new FavItemEqualityComparer()).ToArray();
					foreach (var item in olds)
					{
						FavoriteList.Remove(item);
					}
					var news = fav.Except(FavoriteList, new FavItemEqualityComparer()).ToArray();
					foreach (var item in fav)
					{
						if (!FavoriteList.Any(f=>f.VolumeId == item.VolumeId))
							FavoriteList.Add(item);
					}
				}
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Sync User Favorite : " + exception.Message);
				if (FavoriteList == null)
				{
					FavoriteList = new ObservableCollection<FavourVolume>();
				}
			}
		}

		public async Task ClearUserInfoAsync()
		{
			await LightKindomHtmlClient.LogoutAsync();
			await CachedClient.ClearUserFavoriteCacheAsync();
		}

		public async Task<bool> AddUserFavriteAsync(string volId)
		{

			if (FavoriteList.Any(fav => fav.VolumeId == volId))
				return true;
			try
			{
				var result = await LightKindomHtmlClient.AddUserFavoriteVolume(volId);
				if (!result)
					return false;
				await SyncFavoriteListAsync(true);
				return true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Add User Favorite : " + exception.Message);
			}
			return false;
		}
		public async Task<bool> AddUserFavriteAsync(Volume vol, string seriesTitle = null)
		{

			if (FavoriteList == null)
				return false;
			if (FavoriteList.Any(fav => fav.VolumeId == vol.Id))
				return true;
			try
			{
				var result = await LightKindomHtmlClient.AddUserFavoriteVolume(vol.Id);
				if (!result)
					return false;
				FavourVolume favol = new FavourVolume
				{
					VolumeId = vol.Id,
					FavId = null,
					VolumeNo = vol.VolumeNo.ToString(),
					CoverImageUri = vol.CoverImageUri,
					Description = vol.Description,
					VolumeTitle = vol.Title,
					SeriesTitle = seriesTitle,
					FavTime = DateTime.Now.AddSeconds(-5)
				};
				FavoriteList.Add(favol);
				CachedClient.UpdateCachedUserFavoriteVolumes(FavoriteList);
				return true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Add User Favorite : " + exception.Message);
			}
			return false;
		}
		public async Task<bool> RemoveUserFavriteAsync(string[] favIds)
		{
			try
			{
				await LightKindomHtmlClient.DeleteUserFavorite(favIds);
				foreach (var favId in favIds)
				{
					var f = FavoriteList.FirstOrDefault(fa => fa.FavId == favId);
					if (f != null)
						FavoriteList.Remove(f);
				}
				CachedClient.UpdateCachedUserFavoriteVolumes(FavoriteList);
				return true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Remove User Favorite : " + exception.Message);
			}
			return false;
		}

		public async Task<bool> RemoveUserFavriteAsync(string favId)
		{
			var vol = FavoriteList.FirstOrDefault(fav => fav.FavId == favId);
			if (String.IsNullOrEmpty(favId) || vol == null)
				return false;
			try
			{
				await LightKindomHtmlClient.DeleteUserFavorite(favId);
				FavoriteList.Remove(vol);
				CachedClient.UpdateCachedUserFavoriteVolumes(FavoriteList);
				return true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Remove User Favorite : " + exception.Message);
			}
			return false;
		}

		public async Task SyncRecentListAsync()
		{
			var recent = await LightKindomHtmlClient.GetUserRecentViewedVolumesAsync();
			RecentList = recent.Select(item => new BookmarkInfo { Position = new NovelPositionIdentifier { VolumeId = item.Id }, VolumeTitle = item.Title }).ToList();
		}

	}

}
