using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NowPlaying.Utils
{
    public static class CollectionsUtils
    {
        /// <summary>
        /// Sorted add for observable collection using custom comparer
        /// </summary>
        public static void AddSorted<T>(ObservableCollection<T> collection, T item, IComparer<T> comparer)
        {
            var sortableList = new List<T>(collection);
            int index = sortableList.BinarySearch(item, comparer);
            if (index < 0)
            {
                collection.Insert(~index, item);
            }
            else
            {
                throw new InvalidOperationException($"Observable collection already contained item {item}.");
            }
        }
    
    }
}
