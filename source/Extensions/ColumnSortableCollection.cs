using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NowPlaying.Extensions
{
    public class ColumnSortableCollection<T> : ObservableCollection<T> where T : ColumnSortableObject
    {
        public event SortableColumnsChangedHandler SortableColumnsChanged;

        public void AddSortedItem(T item, IComparer<T> comparer)
        { 
            var sortableList = new List<T>(this);
            int index = sortableList.BinarySearch(item, comparer);
            if (index < 0)
            {
                Insert(~index, item);
                item.SortableColumnsChanged += Item_SortableColumnsChanged;
            }
            else
            {
                throw new InvalidOperationException($"Collection already contained item {item}.");
            }
        }

        public void RemoveItem(T item)
        {
            Remove(item);
            item.SortableColumnsChanged -= Item_SortableColumnsChanged;
        }

        private void Item_SortableColumnsChanged(object sender, SortableColumnsChangedArgs e)
        {
            SortableColumnsChanged?.Invoke(this, e);
        }
    }
}
