using System;
using System.Collections.Generic;

namespace NowPlaying.Extensions
{
    public class SortableColumnsChangedArgs : EventArgs
    {
        public string itemId;
        public string[] changedColumns;
        public SortableColumnsChangedArgs(string itemId, string[] changedColumns)
        {
            this.itemId = itemId;
            this.changedColumns = changedColumns;
        }
    }
    public delegate void SortableColumnsChangedHandler(object sender, SortableColumnsChangedArgs e);

    public abstract class ColumnSortableObject : ObservableObject
    {
        public event SortableColumnsChangedHandler SortableColumnsChanged;

        public void OnSortableColumnChanged(string itemId, string changedColumn)
        {
            SortableColumnsChanged?.Invoke(this, new SortableColumnsChangedArgs(itemId, new string[] { changedColumn }));
        }

        public void OnSortableColumnsChanged(string itemId, string[] changedColumns)
        {
            SortableColumnsChanged?.Invoke(this, new SortableColumnsChangedArgs(itemId, changedColumns));
        }
    }

}
