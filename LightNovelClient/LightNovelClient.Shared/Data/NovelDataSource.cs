using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace LightNovel.Data
{
    interface INovelProvider
    {
        IAsyncOperation<IEnumerable<string>> GetSeriesIndex();
        IAsyncOperation<Chapter> GetChapterAsync(string chpId, string volId, string serId);
        IAsyncOperation<Volume>  GetVolumeAsync(string vid, bool forceRefresh = false);
        IAsyncOperation<Series>  GetSeriesAsync(string sid, bool forceRefresh = false);
    }
}
