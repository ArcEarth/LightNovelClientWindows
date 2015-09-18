using LightNovel.Common;
using LightNovel.Service;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.Web.Http;

namespace LightNovel
{
    static class AppGlobal
    {
        static AppGlobal()
        {
            Settings = new ApplicationSettings();
            IsHistoryListChanged = true;
            SecondaryViews = new ObservableCollection<ViewLifetimeControl>();
            LightKindomHtmlClient.AccountUserAgent = Settings.DeviceUserAgent;
        }


        public static ApplicationSettings Settings { get; }
        public static UserInfo User;

        public static ObservableCollection<ViewLifetimeControl> SecondaryViews = new ObservableCollection<ViewLifetimeControl>();

        private const string LocalRecentFilePath = "history.json";
        private const string LocalBookmarkFilePath = "bookmark.json";

        public static bool IsSignedIn => User != null && !User.Credential.Expired && !string.IsNullOrEmpty(Settings.DeviceUserAgent);

        public static bool IsHistoryListChanged { get; set; }

        public static bool IsBookmarkListChanged { get; set; }

        public static List<BookmarkInfo> RecentList { get; set; }

        public static List<BookmarkInfo> BookmarkList { get; set; }

        public static StorageFolder IllustrationFolder { get; set; }

        public static event EventHandler RecentListChanged;
        public static event EventHandler BookmarkListChanged;

        public static void NotifyRecentsChanged()
        {
            if (RecentListChanged != null)
                RecentListChanged(App.Current, null);
        }

        public static void NotifyBookmarksChanged()
        {
            if (BookmarkListChanged != null)
                BookmarkListChanged(App.Current, null);
        }

        public static async Task UpdateHistoryListAsync(BookmarkInfo bookmark)
        {
            if (RecentList == null)
                await LoadHistoryDataAsync();
            //var existed = RecentList.FirstOrDefault(item => item.Position.SeriesId == bookmark.Position.SeriesId);

            //// No Changes
            //if (bookmark.Position.VolumeNo == existed.Position.VolumeNo && bookmark.Position.ChapterNo == existed.Position.ChapterNo && bookmark.Position.LineNo == existed.Position.LineNo)
            //    return;
            RecentList.RemoveAll(item => item.Position.SeriesId == bookmark.Position.SeriesId);
            RecentList.Add(bookmark);
            IsHistoryListChanged = true;
            await SaveHistoryDataAsync();
            NotifyRecentsChanged();
        }

        public static async Task<bool> UpdateSecondaryTileAsync(BookmarkInfo bookmark)
        {
            if (SecondaryTile.Exists(bookmark.Position.SeriesId))
            {
                var tile = new SecondaryTile(bookmark.Position.SeriesId);
                string args = bookmark.Position.ToString();
                tile.Arguments = args;
                var result = await tile.UpdateAsync();
                return true;
            }
            return false;
        }

        private async static Task<T> GetFromLocalFolderAsAsync<T>(string filePath) where T : class
        {
            try
            {
                var file = await Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(filePath);
                var content = await Windows.Storage.FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return null;
            }
        }
        private async static Task<T> GetFromRoamingFolderAsAsync<T>(string filePath) where T : class
        {
            try
            {
                var file = await Windows.Storage.ApplicationData.Current.RoamingFolder.GetFileAsync(filePath);
                var content = await Windows.Storage.FileIO.ReadTextAsync(file);
                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return null;
            }
        }
        private static async Task SaveToLocalFolderAsync(object obj, string path)
        {
            var file = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFileAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists);
            var content = JsonConvert.SerializeObject(obj);
            await Windows.Storage.FileIO.WriteTextAsync(file, content);
        }
        private static async Task<bool> SaveToRoamingFolderAsync(object obj, string path)
        {
            try
            {
                var file = await Windows.Storage.ApplicationData.Current.RoamingFolder.CreateFileAsync(path, Windows.Storage.CreationCollisionOption.OpenIfExists);
                var content = JsonConvert.SerializeObject(obj);
                await Windows.Storage.FileIO.WriteTextAsync(file, content);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //public Task loadHistoryDataTask = null;
        //public Task loadBookmarkDataTask = null;
        public static async Task LoadHistoryDataAsync()
        {
 
            try
            {
                if (RecentList != null)
                    return;

                RecentList = await GetFromRoamingFolderAsAsync<List<BookmarkInfo>>(LocalRecentFilePath);

                if (RecentList == null)
                {
                    RecentList = await GetFromLocalFolderAsAsync<List<BookmarkInfo>>(LocalRecentFilePath);
                }
            }
            catch (Exception excp)
            {
                Debug.WriteLine(excp.Message);
            }

            if (RecentList == null)
                RecentList = new List<BookmarkInfo>();
        }

        public static async Task SaveHistoryDataAsync()
        {
            if (RecentList.Count > 30)
                RecentList.RemoveRange(0, RecentList.Count - 30);
            if (RecentList != null)
            {
                await SaveToRoamingFolderAsync(RecentList, LocalRecentFilePath);
                IsHistoryListChanged = false;
            }
        }
        public static async Task LoadBookmarkDataAsync()
        {
            if (BookmarkList != null)
                return;
            BookmarkList = await GetFromRoamingFolderAsAsync<List<BookmarkInfo>>(LocalBookmarkFilePath);

            if (BookmarkList == null)
                BookmarkList = new List<BookmarkInfo>();

        }
        public static async Task SaveBookmarkDataAsync()
        {
            if (BookmarkList.Count > 100)
                BookmarkList.RemoveRange(0, BookmarkList.Count - 100);
            if (BookmarkList != null)
            {
                await SaveToRoamingFolderAsync(BookmarkList, LocalBookmarkFilePath);
            }
        }

        public static async Task PullBookmarkFromUserFavoriteAsync(bool forectRefresh = false, bool forceSyncFromCloud = false)
        {
            if (BookmarkList == null)
                await LoadBookmarkDataAsync();
            if (User != null)
            {
                // guard about login data
                return;

                await User.SyncFavoriteListAsync(forectRefresh);
                var favList = from fav in User.FavoriteList orderby fav.VolumeId group fav by fav.SeriesTitle;
                bool Changed = false;

                if (forceSyncFromCloud)
                {
                    for (int i = 0; i < BookmarkList.Count; i++)
                    {
                        var bk = BookmarkList[i];
                        if (!favList.Any(g => g.First().SeriesTitle == bk.SeriesTitle))
                        {
                            BookmarkList.RemoveAt(i--);
                            Changed = true;
                        }
                    }
                }

                foreach (var series in favList)
                {
                    var vol = series.LastOrDefault();
                    if (BookmarkList.Any(bk => bk.SeriesTitle == vol.SeriesTitle))
                        continue;
                    var item = new BookmarkInfo { SeriesTitle = vol.SeriesTitle, VolumeTitle = vol.VolumeTitle, ViewDate = vol.FavTime };
                    item.Position = new NovelPositionIdentifier { /*SeriesId = volume.ParentSeriesId,*/ VolumeId = vol.VolumeId, VolumeNo = -1 };
                    BookmarkList.Add(item);
                    Changed = true;
                }
                if (Changed)
                    await SaveBookmarkDataAsync();
            }
        }

        public static async Task PushBookmarkToUserFavortiteAsync(bool forceSyncFromCloud = false)
        {
            //if (forceSyncFromCloud)
            //{
            //	var favList = from fav in User.FavoriteList orderby fav.VolumeId group fav by fav.SeriesTitle;
            //	List<string> 
            //	foreach (var series in favList)
            //	{
            //		var vol = series.LastOrDefault();
            //		if (BookmarkList.Any(bk => bk.SeriesTitle == vol.SeriesTitle))
            //			continue;

            //	}
            //}
            foreach (var bk in BookmarkList)
            {
                if (!User.FavoriteList.Any(fav => bk.Position.VolumeId == fav.VolumeId))
                {
                    var result = await LightKindomHtmlClient.AddUserFavoriteVolume(bk.Position.VolumeId);
                }
            }
        }

        public static void SetAccountUserAgent(string ua)
        {
            LightKindomHtmlClient.AccountUserAgent = ua;
            Settings.DeviceUserAgent = ua;
        }


        public static async Task<UserInfo> SignInAsync(string userName, string password)
        {
            try
            {
                var session = await LightKindomHtmlClient.LoginAsync(userName, password);
                if (session == null)
                    return null;
                Settings.Credential = session;
                Settings.SetUserNameAndPassword(userName, password);
                User = new UserInfo { UserName = userName, Password = password, Credential = session };
            }
            catch (Exception)
            {
                return null;
            }
            return User;
        }

        public static async Task<bool> SignInAutomaticllyAsync(bool forecRefresh = false)
        {
            var userName = Settings.UserName;
            var session = Settings.Credential;
            var password = Settings.Password;
            if (String.IsNullOrEmpty(password) || String.IsNullOrEmpty(userName))
                return false;
            try
            {
                if (!forecRefresh && !session.Expired)
                {
                    LightKindomHtmlClient.Credential = session;
                    User = new UserInfo { UserName = userName, Password = password, Credential = session };
                }
                else
                {
                    var newSession = await LightKindomHtmlClient.LoginAsync(userName, password);
                    if (newSession == null)
                        return false;
                    Settings.Credential = newSession;
                    User = new UserInfo { UserName = userName, Password = password, Credential = newSession };
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> SignOutAsync()
        {
            if (!IsSignedIn) return true;
            Settings.SetUserNameAndPassword("", "");
            Settings.Credential = new Session();
            var clearTask = User.ClearUserInfoAsync();
            await clearTask;
            User = null;
            return true;
        }

        public static bool IsConnectedToInternet()
        {
            ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            return (connectionProfile != null && connectionProfile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess);
        }

        static bool _shouldAutoImage = true;
        public static bool ShouldAutoLoadImage
        {
            get
            {
                return _shouldAutoImage;
            }
        }

        public static void RefreshAutoLoadPolicy()
        {
            if (Settings.ImageLoadingPolicy == ImageLoadingPolicy.Automatic)
                _shouldAutoImage = true;
            else if (Settings.ImageLoadingPolicy == ImageLoadingPolicy.Manual)
                _shouldAutoImage = false;
            else
                _shouldAutoImage = NetworkState == AppNetworkState.Unrestricted;
        }

        internal static void NetworkInformation_NetworkStatusChanged(object sender)
        {
            RefreshAutoLoadPolicy();
        }

        public enum AppNetworkState
        {
            Unconnected = 0,
            Metered = 1,
            Unrestricted = 2
        };

        public static AppNetworkState NetworkState
        {
            get
            {
                ConnectionProfile connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile == null || connectionProfile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.InternetAccess)
                    return AppNetworkState.Unconnected;
                if (connectionProfile.GetConnectionCost().NetworkCostType == NetworkCostType.Unrestricted)
                    return AppNetworkState.Unrestricted;
                else
                    return AppNetworkState.Metered;
            }
        }

        //public List<FavourVolume> FavoriteList { get; set; }

        public static BitmapTransform CreateUniformToFillTransform(Size OriginalSize, Size NewSize, HorizontalAlignment hAlign = HorizontalAlignment.Center, VerticalAlignment vAlign = VerticalAlignment.Center)
        {
            var transform = new BitmapTransform();
            double wScale = (double)NewSize.Width / (double)OriginalSize.Width;
            double hScale = (double)NewSize.Height / (double)OriginalSize.Height;
            double uScale = Math.Max(wScale, hScale);
            transform.ScaledWidth = (uint)Math.Round(uScale * OriginalSize.Width);
            transform.ScaledHeight = (uint)Math.Round(uScale * OriginalSize.Height);
            BitmapBounds bound;
            bound.X = 0;
            bound.Y = 0;
            bound.Width = (uint)NewSize.Width;
            bound.Height = (uint)NewSize.Height;
            if (wScale > hScale) // Crop in height
            {
                if (vAlign == VerticalAlignment.Bottom)
                    bound.Y = transform.ScaledHeight - bound.Height;
                else if (vAlign == VerticalAlignment.Center)
                    bound.Y = (transform.ScaledHeight - bound.Height) / 2;
            }
            else
            {
                if (hAlign == HorizontalAlignment.Right)
                    bound.Y = transform.ScaledWidth - bound.Width;
                else if (hAlign == HorizontalAlignment.Center)
                    bound.Y = (transform.ScaledWidth - bound.Width) / 2;
            }
            return transform;
        }

        struct TileLogoGroup
        {
            public Uri Square150x150Logo { get; set; }
            public Uri Square30x30Logo { get; set; }
            public Uri Square310x310Logo { get; set; }
            public Uri Square70x70Logo { get; set; }
            public Uri Wide310x150Logo { get; set; }
        }
        public static async Task<Uri> CreateTileImageAsync(Uri imageUri, string fileName = null, TileSize tileSize = TileSize.Square150x150)
        {
            //BitmapImage bitmap = new BitmapImage(imageUri);
            Size imgSize;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = imageUri.LocalPath;
                fileName = Path.GetFileName(fileName);
                string sizeSuffix = "-150";
                switch (tileSize)
                {
                    default:
                    case TileSize.Default:
                    case TileSize.Square150x150:
                        sizeSuffix = "-150";
                        imgSize.Height = 150;
                        imgSize.Width = 150;
                        break;
                    case TileSize.Square30x30:
                        sizeSuffix = "-30";
                        imgSize.Width = 30;
                        imgSize.Height = 30;
                        break;
                    case TileSize.Square310x310:
                        sizeSuffix = "-310";
                        imgSize.Width = 310;
                        imgSize.Height = 310;
                        break;
                    //case TileSize.Square70x70:
                    //	sizeSuffix = "-70";
                    //	imgSize.Width = 70;
                    //	imgSize.Height = 70;
                    //	break;
                    case TileSize.Wide310x150:
                        sizeSuffix = "-310x150";
                        imgSize.Width = 310;
                        imgSize.Height = 150;
                        break;

                }
                fileName = Path.GetFileNameWithoutExtension(fileName) + sizeSuffix + Path.GetExtension(fileName);
            }
            var localUri = new Uri(string.Format("ms-appdata:///local/illustration/{0}", fileName));

            if (IllustrationFolder == null)
                IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);

            StorageFile file = null;

            try
            {
                file = await IllustrationFolder.GetFileAsync(fileName);
                return localUri;
            }
            catch (FileNotFoundException)
            {
            }

            try
            {
                using (var client = new HttpClient())
                {
                    using (var stream = await client.GetInputStreamAsync(imageUri))
                    {
                        using (var memstream = new InMemoryRandomAccessStream())
                        {
                            await stream.AsStreamForRead().CopyToAsync(memstream.AsStreamForWrite());
                            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(memstream);


                            file = await IllustrationFolder.CreateFileAsync(fileName);

                            using (var targetStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                            {
                                BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(targetStream, decoder);

                                var transform = CreateUniformToFillTransform(new Size(decoder.PixelWidth, decoder.PixelHeight), imgSize);
                                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Cubic;
                                encoder.BitmapTransform.ScaledHeight = transform.ScaledHeight;
                                encoder.BitmapTransform.ScaledWidth = transform.ScaledWidth;
                                encoder.BitmapTransform.Bounds = transform.Bounds;
                                await encoder.FlushAsync();
                                //WriteableBitmap wbp = new WriteableBitmap(150,150);
                                //await wbp.SetSourceAsync(memstream);
                                //await wbp.SaveToFile(Current.IllustrationFolder, fileName);
                                return localUri;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }
            return null;
        }
        public static async Task<Uri> CacheIllustrationAsync(Uri internetUri, string fileName = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = internetUri.LocalPath;
                fileName = Path.GetFileName(fileName);
            }

            if (IllustrationFolder == null)
                IllustrationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("illustration", CreationCollisionOption.OpenIfExists);

            StorageFile file = null;

            var localUri = new Uri(string.Format("ms-appdata:///local/illustration/{0}", fileName));

            try
            {
                file = await IllustrationFolder.GetFileAsync(fileName);
                return localUri;
            }
            catch (FileNotFoundException)
            {
            }

            try
            {
                using (var response = await System.Net.HttpWebRequest.CreateHttp(internetUri).GetResponseAsync())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        file = await IllustrationFolder.CreateFileAsync(fileName);
                        using (var filestream = await file.OpenStreamForWriteAsync())
                        {
                            await stream.CopyToAsync(filestream);
                            return localUri;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

    }
}
