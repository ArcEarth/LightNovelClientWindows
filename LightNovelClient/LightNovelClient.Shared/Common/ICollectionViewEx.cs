using System;
using System.Collections;
using System.Collections.Generic;
using Windows.UI.Xaml.Data;

namespace LightNovel.Common
{
    //--------------------------------------------------------------------------------
    #region ** ICollectionViewEx

    /// <summary>
    /// Extends the WinRT ICollectionView to provide sorting and filtering.
    /// </summary>
    public interface ICollectionViewEx : ICollectionView
    {
        /// <summary>
        /// Gets a value that indicates whether this view supports filtering via the
        /// <see cref="Filter"/> property.
        /// </summary>
        bool CanFilter { get; }
        /// <summary>
        /// Gets or sets a callback used to determine if an item is suitable for inclusion
        /// in the view.
        /// </summary>
        Predicate<object> Filter { get; set; }
        /// <summary>
        /// Gets a value that indicates whether this view supports sorting via the 
        /// <see cref="SortDescriptions"/> property.
        /// </summary>
        bool CanSort { get; }
        /// <summary>
        /// Gets a collection of System.ComponentModel.SortDescription objects that describe
        /// how the items in the collection are sorted in the view.
        /// </summary>
        IList<SortDescription> SortDescriptions { get; }
        /// <summary>
        /// Gets a value that indicates whether this view supports grouping via the 
        /// <see cref="GroupDescriptions"/> property.
        /// </summary>
        bool CanGroup { get; }
        /// <summary>
        /// Gets a collection of System.ComponentModel.GroupDescription objects that
        /// describe how the items in the collection are grouped in the view.
        /// </summary>
        IList<object> GroupDescriptions { get; }
        /// <summary>
        /// Get the underlying collection.
        /// </summary>
        IEnumerable SourceCollection { get; }
        /// <summary>
        /// Enters a defer cycle that you can use to merge changes to the view and delay
        /// automatic refresh.
        /// </summary>
        IDisposable DeferRefresh();
        /// <summary>
        /// Refreshes the view applying the current sort and filter conditions.
        /// </summary>
        void Refresh();
    }
    public class SortDescription
    {
        public SortDescription(string propertyName, ListSortDirection direction)
        {
            PropertyName = propertyName;
            Direction = direction;
        }
        public string PropertyName { get; set; }
        public ListSortDirection Direction { get; set; }
    }
    public enum ListSortDirection
    {
        Ascending = 0,
        Descending = 1,
    }
    #endregion

    //--------------------------------------------------------------------------------
    #region ** IEditableCollectionView

    /// <summary>
    /// Implements a WinRT version of the IEditableCollectionView interface.
    /// </summary>
    public interface IEditableCollectionView
    {
        bool CanAddNew { get; }
        bool CanRemove { get; }
        bool IsAddingNew { get; }
        object CurrentAddItem { get; }
        object AddNew();
        void CancelNew();
        void CommitNew();

        bool CanCancelEdit { get; }
        bool IsEditingItem { get; }
        object CurrentEditItem { get; }
        void EditItem(object item);
        void CancelEdit();
        void CommitEdit();
    }

    #endregion
}
