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

		public void AddFavorite(string volumeId)
		{
			FavoriteList.Add(new FavourVolume
			{
				VolumeId = volumeId,
			});
		}
		public void RemoveFavorite(string volId)
		{
			var volume = FavoriteList.FirstOrDefault(vol => vol.VolumeId == volId);
			if (volume != null)
				FavoriteList.Remove(volume);
		}
		public void AddRecent(BookmarkInfo bookmark)
		{

		}

		public async Task SyncFavoriteListAsync(bool forceRefresh = false)
		{
			try
			{

				var fav = await CachedClient.GetUserFavoriteVolumesAsync(!IsUserFavoriteValiad || forceRefresh);
				if (FavoriteList == null)
				{
					FavoriteList = new ObservableCollection<FavourVolume>(fav);
					//FavoriteList.CollectionChanged += FavoriteList_CollectionChanged;
				}
				else
				{
					//FavoriteList.CollectionChanged -= FavoriteList_CollectionChanged;
					FavoriteList.Clear();
					foreach (var item in fav)
					{
						FavoriteList.Add(item);
					}
					//FavoriteList.CollectionChanged += FavoriteList_CollectionChanged;
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
			LightKindomHtmlClient.Logout();
			await CachedClient.ClearUserFavoriteCacheAsync();
		}

		public async Task<bool> AddUserFavriteAsync(Volume vol , string seriesTitle = "Untitled")
		{

			if (FavoriteList.Any(fav => fav.VolumeId == vol.Id))
				return true;
			try
			{
				var favId = await LightKindomHtmlClient.AddUserFavoriteVolume(vol.Id);
				await SyncFavoriteListAsync(true);
				//FavourVolume favol = new FavourVolume
				//{
				//	VolumeId = vol.Id,
				//	FavId = favId,
				//	VolumeNo = vol.VolumeNo.ToString(),
				//	CoverImageUri = vol.CoverImageUri,
				//	Description = vol.Description,
				//	VolumeTitle = vol.Title,
				//	SeriesTitle = seriesTitle,
				//	FavTime = DateTime.Now.AddSeconds(-5)
				//};
				//FavoriteList.Add(favol);
				//CachedClient.UpdateCachedUserFavoriteVolumes(FavoriteList);
				return true;
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Add User Favorite : " + exception.Message);
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

		[Deprecated("This should not be use", DeprecationType.Deprecate, 100859904)]
		async void FavoriteList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Move)
				return;
			try
			{
				CachedClient.UpdateCachedUserFavoriteVolumes(FavoriteList);
				if (e.NewItems != null)
					foreach (FavourVolume vol in e.NewItems)
					{
						if (String.IsNullOrEmpty(vol.FavId))
							await LightKindomHtmlClient.AddUserFavoriteVolume(vol.VolumeId);
					}
				if (e.OldItems != null)
					foreach (FavourVolume vol in e.OldItems)
					{
						if (!String.IsNullOrEmpty(vol.FavId))
							await LightKindomHtmlClient.DeleteUserFavorite(vol.FavId);
					}
			}
			catch (Exception exception)
			{
				Debug.WriteLine("Error : Failed to Sync User Favorite : " + exception.Message);
			}
		}

		public async Task SyncRecentListAsync()
		{
			var recent = await LightKindomHtmlClient.GetUserRecentViewedVolumesAsync();
			RecentList = recent.Select(item => new BookmarkInfo { Position = new NovelPositionIdentifier { VolumeId = item.Id }, VolumeTitle = item.Title }).ToList();
		}

	}

}
