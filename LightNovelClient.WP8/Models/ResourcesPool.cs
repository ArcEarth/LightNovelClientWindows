using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightNovel.Service
{
    static class LightNovelService
    {
        private static readonly Dictionary<string, Volume> VolumeDictionary = new Dictionary<string, Volume>();
        private static readonly Dictionary<string, Series> SeriesDictionary = new Dictionary<string, Series>();
        private static readonly Dictionary<string, Chapter> ChaptersDictionary = new Dictionary<string, Chapter>();

        static public Volume Volume(string id)
        {
            if (VolumeDictionary.ContainsKey(id))
                return VolumeDictionary[id];
            return null;
        }

        static public Task<Volume> GetVolumeAsync(string id)
        {
            if (VolumeDictionary.ContainsKey(id))
                return Task.Run(() => VolumeDictionary[id]);
            return LightKindomHtmlClient.GetVolumeAsync(id);
        }

        static public Chapter Chapter(string id)
        {
            if (ChaptersDictionary.ContainsKey(id))
                return ChaptersDictionary[id];
            return null;
        }
        static public Series Series(string id)
        {
            if (SeriesDictionary.ContainsKey(id))
                return SeriesDictionary[id];
            return null;
        }

    }
}
