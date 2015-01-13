using Microsoft.Phone.Globalization;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LightNovel
{
    public class KeyGroup<TKey, TValue> : List<TValue>, IGrouping<TKey,TValue>
    {
        public TKey Key { get; set; }
    }

    public class AlphaKeyGroup<T> : List<T>
    {
        private const string GlobeGroupKey = "\uD83C\uDF10";

        /// <summary>
        /// The delegate that will be used to obtain the key information.
        /// </summary>
        /// <param name="item">An object of type T</param>
        /// <returns>The key value to use for this object</returns>
        public delegate string GetKeyDelegate(T item);

        /// <summary>
        /// The Key of this group.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Public ctor.
        /// </summary>
        /// <param name="key">The key for this group.</param>
        public AlphaKeyGroup()
        {
        }
        public AlphaKeyGroup(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Public ctor.
        /// </summary>
        /// <param name="grouping">The Linq grouping. N.B. this will enumerate all items.</param>
        public AlphaKeyGroup(IGrouping<string, T> grouping)
        {
            Key = grouping.Key;
            this.AddRange(grouping);
        }

        /// <summary>
        /// Create a list of AlphaGroup<T> with keys set by a SortedLocaleGrouping.
        /// </summary>
        /// <param name="items">The items to place in the groups.</param>
        /// <param name="ci">The CultureInfo to group and sort by.</param>
        /// <param name="getKey">A delegate to get the key from an item.</param>
        /// <param name="sort">Will sort the data if true.</param>
        /// <returns>An items source for a LongListSelector</returns>
        public static List<AlphaKeyGroup<T>> CreateGroups(IEnumerable<T> items, CultureInfo ci, GetKeyDelegate getKey, bool sort)
        {
            SortedLocaleGrouping slg = new SortedLocaleGrouping(ci);
			List<AlphaKeyGroup<T>> list = new List<AlphaKeyGroup<T>>();

			foreach (string key in slg.GroupDisplayNames)
			{
				if (key == "...")
				{
					list.Add(new AlphaKeyGroup<T>(GlobeGroupKey));
				}
				else
				{
					list.Add(new AlphaKeyGroup<T>(key));
				}
			}

            foreach (T item in items)
            {
                int index = slg.GetGroupIndex(getKey(item));
                if (index >= 0 && index < list.Count)
                {
                    list[index].Add(item);
                }
            }

            if (sort)
            {
                foreach (AlphaKeyGroup<T> group in list)
                {
                    group.Sort((c0, c1) => { return ci.CompareInfo.Compare(getKey(c0), getKey(c1)); });
                }
            }

            return list;
        }
    }
}