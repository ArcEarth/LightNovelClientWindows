using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Windows.UI.Xaml.Data;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace LightNovel.Common
{
    /// <summary>
    /// Simple implementation of the <see cref="ICollectionViewEx"/> interface, 
    /// which extends the standard WinRT definition of the <see cref="ICollectionView"/> 
    /// interface to add sorting and filtering.
    /// </summary>
    /// <remarks>
    /// Here's an example that shows how to use the <see cref="ListCollectionView"/> class:
    /// <code>
    /// // create a simple list
    /// var list = new List&lt;Rect&gt;();
    /// for (int i = 0; i &lt; 200; i++)
    ///   list.Add(new Rect(i, i, i, i));
    ///   
    /// // create collection view based on list
    /// var cv = new ListCollectionView();
    /// cv.Source = list;
    /// 
    /// // apply filter
    /// cv.Filter = (item) =&gt; { return ((Rect)item).X % 2 == 0; };
    /// 
    /// // apply sort
    /// cv.SortDescriptions.Add(new SortDescription("Width", ListSortDirection.Descending));
    /// 
    /// // show data on grid
    /// mygrid.ItemsSource = cv;
    /// </code>
    /// </remarks>
    public class ListCollectionView :
        ICollectionViewEx,
        IEditableCollectionView,
        INotifyPropertyChanged,
        IComparer<object>
    {
        //------------------------------------------------------------------------------------
        #region ** fields

        object _source;                                 // original data source
        IList _sourceList;                              // original data source as list
        Type _itemType;                                 // type of item in the source collection
        INotifyCollectionChanged _sourceNcc;            // listen to changes in the source
        List<object> _view;                             // filtered/sorted data source
        ObservableCollection<SortDescription> _sort;    // sorting parameters
        Dictionary<string, PropertyInfo> _sortProps;    // PropertyInfo dictionary used while sorting
        Predicate<object> _filter;                      // filter
        int _index;                                     // cursor position
        int _updating;                                  // suspend notifications

        #endregion

        //------------------------------------------------------------------------------------
        #region ** ctor

        public ListCollectionView(object source)
        {
            // view exposed to consumers
            _view = new List<object>();

            // sort descriptor collection
            _sort = new ObservableCollection<SortDescription>();
            _sort.CollectionChanged += _sort_CollectionChanged;
            _sortProps = new Dictionary<string, PropertyInfo>();

            // hook up to data source
            Source = source;
        }
        public ListCollectionView() : this(null) { }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** object model

        /// <summary>
        /// Gets or sets the collection from which to create the view.
        /// </summary>
        public object Source
        {
            get { return _source; }
            set
            {
                if (_source != value)
                {
                    // save new source
                    _source = value;

                    // save new source as list (so we can add/remove etc)
                    _sourceList = value as IList;

                    // get the type of object in the collection
                    _itemType = GetItemType();

                    // listen to changes in the source
                    if (_sourceNcc != null)
                    {
                        _sourceNcc.CollectionChanged -= _sourceCollectionChanged;
                    }
                    _sourceNcc = _source as INotifyCollectionChanged;
                    if (_sourceNcc != null)
                    {
                        _sourceNcc.CollectionChanged += _sourceCollectionChanged;
                    }

                    // refresh our view
                    HandleSourceChanged();

                    // inform listeners
                    OnPropertyChanged("Source");
                }
            }
        }
        /// <summary>
        /// Update the view from the current source, using the current filter and sort settings.
        /// </summary>
        public void Refresh()
        {
            HandleSourceChanged();
        }
        /// <summary>
        /// Raises the <see cref="CurrentChanging"/> event.
        /// </summary>
        protected virtual void OnCurrentChanging(CurrentChangingEventArgs e)
        {
            if (_updating <= 0)
            {
                if (CurrentChanging != null)
                    CurrentChanging(this, e);
            }
        }
        /// <summary>
        /// Raises the <see cref="CurrentChanged"/> event.
        /// </summary>
        protected virtual void OnCurrentChanged(object e)
        {
            if (_updating <= 0)
            {
                if (CurrentChanged != null)
                    CurrentChanged(this, e);
                OnPropertyChanged("CurrentItem");
            }
        }
        /// <summary>
        /// Raises the <see cref="VectorChanged"/> event.
        /// </summary>
        protected virtual void OnVectorChanged(IVectorChangedEventArgs e)
        {
            if (this.IsAddingNew || this.IsEditingItem)
            {
                throw new NotSupportedException("Cannot change collection while adding or editing items.");
            }

            if (_updating <= 0)
            {
                if (VectorChanged != null)
                    VectorChanged(this, e);
                OnPropertyChanged("Count");
            }
        }
        /// <summary>
        /// Enters a defer cycle that you can use to merge changes to the view and delay
        /// automatic refresh.
        /// </summary>
        public IDisposable DeferRefresh()
        {
            return new DeferNotifications(this);
        }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** event handlers

        // the original source has changed, update our source list
        void _sourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_updating <= 0)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems.Count == 1)
                        {
                            HandleItemAdded(e.NewStartingIndex, e.NewItems[0]);
                        }
                        else
                        {
                            HandleSourceChanged();
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems.Count == 1)
                        {
                            HandleItemRemoved(e.OldStartingIndex, e.OldItems[0]);
                        }
                        else
                        {
                            HandleSourceChanged();
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Reset:
                        HandleSourceChanged();
                        break;
                    default:
                        throw new Exception("Unrecognized collection change notification: " + e.Action.ToString());
                }
            }
        }

        // sort changed, refresh view
        void _sort_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_updating <= 0)
            {
                HandleSourceChanged();
            }
        }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** implementation

        // add item to view
        void HandleItemAdded(int index, object item)
        {
            // if the new item is filtered out of view, no work
            if (_filter != null && !_filter(item))
            {
                return;
            }

            // compute insert index
            if (_sort.Count > 0)
            {
                // sorted: insert at sort position
                _sortProps.Clear();
                index = _view.BinarySearch(item, this);
                if (index < 0) index = ~index;
            }
            else if (_filter != null)
            {
                // if the source is not a list (e.g. enum), then do a full refresh
                if (_sourceList == null)
                {
                    HandleSourceChanged();
                    return;
                }

                // find insert index
                // count invisible items below the insertion point and
                // subtract from the number of items in the view
                // (counting from the bottom is more efficient for the
                // most common case which is appending to the source collection)
                var visibleBelowIndex = 0;
                for (int i = index; i < _sourceList.Count; i++)
                {
                    if (!_filter(_sourceList[i]))
                    {
                        visibleBelowIndex++;
                    }
                }
                index = _view.Count - visibleBelowIndex;
            }

            // add item to view
            _view.Insert(index, item);

            // keep selection on the same item
            if (index <= _index)
            {
                _index++;
            }

            // notify listeners
            var e = new VectorChangedEventArgs(CollectionChange.ItemInserted, index, item);
            OnVectorChanged(e);
        }

        // remove item from view
        void HandleItemRemoved(int index, object item)
        {
            // check if the item is in the view
            if (_filter != null && !_filter(item))
            {
                return;
            }

            // compute index into view
            if (index < 0 || index >= _view.Count || !object.Equals(_view[index], item))
            {
                index = _view.IndexOf(item);
            }
            if (index < 0)
            {
                return;
            }

            // remove item from view
            _view.RemoveAt(index);

            // keep selection on the same item
            if (index <= _index)
            {
                _index--;
            }

            // notify listeners
            var e = new VectorChangedEventArgs(CollectionChange.ItemRemoved, index, item);
            OnVectorChanged(e);
        }

        // update view after changes other than add/remove an item
        void HandleSourceChanged()
        {
            // release sort property PropertyInfo dictionary
            _sortProps.Clear();

            // keep selection if possible
            var currentItem = CurrentItem;

            // re-create view
            _view.Clear();
            var ie = Source as IEnumerable;
            if (ie != null)
            {
                foreach (var item in ie)
                {
                    if (_filter == null || _filter(item))
                    {
                        if (_sort.Count > 0)
                        {
                            var index = _view.BinarySearch(item, this);
                            if (index < 0) index = ~index;
                            _view.Insert(index, item);
                        }
                        else
                        {
                            _view.Add(item);
                        }
                    }
                }
            }

            // release sort property PropertyInfo dictionary
            _sortProps.Clear();

            // notify listeners
            OnVectorChanged(VectorChangedEventArgs.Reset);

            // restore selection if possible
            MoveCurrentTo(currentItem);
        }

        // update view after an item changes (apply filter/sort if necessary)
        void HandleItemChanged(object item)
        {
            // apply filter/sort after edits
            bool refresh = false;
            if (_filter != null && !_filter(item))
            {
                // item was removed from view
                refresh = true;
            }
            if (_sort.Count > 0)
            {
                // find sorted index for this object
                _sortProps.Clear();
                var newIndex = _view.BinarySearch(item, this);
                if (newIndex < 0) newIndex = ~newIndex;

                // item moved within the collection
                if (newIndex >= _view.Count || _view[newIndex] != item)
                {
                    refresh = true;
                }
                else if (newIndex < _view.Count && _view[newIndex] == item)
                {
                    var comparer = (IComparer<object>)this;
                    if (newIndex > 0 &&
                        comparer.Compare(_view[newIndex - 1], _view[newIndex]) > 0)
                    {
                        refresh = true;
                    }
                    else if (newIndex < _view.Count - 1 &&
                        comparer.Compare(_view[newIndex + 1], _view[newIndex]) < 0)
                    {
                        refresh = true;
                    }
                }
            }
            if (refresh)
            {
                HandleSourceChanged();
            }
        }

        // move the cursor to a new position
        bool MoveCurrentToIndex(int index)
        {
            // invalid?
            if (index < -1 || index >= _view.Count)
            {
                return false;
            }

            // no change?
            if (index == _index)
            {
                return false;
            }

            // fire changing
            var e = new CurrentChangingEventArgs();
            OnCurrentChanging(e);
            if (e.Cancel)
            {
                return false;
            }

            // change and fire changed
            _index = index;
            OnCurrentChanged(null);
            return true;
        }

        // get the type of item in the source collection
        Type GetItemType()
        {
            Type itemType = null;
            if (_source != null)
            {
                var type = _source.GetType();
                var args = type.GenericTypeArguments;
                if (args.Length == 1)
                {
                    itemType = args[0];
                }
                else if (_sourceList != null && _sourceList.Count > 0)
                {
                    var item = _sourceList[0];
                    itemType = item.GetType();
                }
            }
            return itemType;
        }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** nested classes

        /// <summary>
        /// Class that handles deferring notifications while the view is modified.
        /// </summary>
        class DeferNotifications : IDisposable
        {
            ListCollectionView _view;
            object _currentItem;

            internal DeferNotifications(ListCollectionView view)
            {
                _view = view;
                _currentItem = _view.CurrentItem;
                _view._updating++;
            }
            public void Dispose()
            {
                _view.MoveCurrentTo(_currentItem);
                _view._updating--;
                _view.Refresh();
            }
        }
        /// <summary>
        /// Class that implements IVectorChangedEventArgs so we can fire VectorChanged events.
        /// </summary>
        class VectorChangedEventArgs : IVectorChangedEventArgs
        {
            CollectionChange _cc = CollectionChange.Reset;
            uint _index = (uint)0xffff;

            static VectorChangedEventArgs _reset = new VectorChangedEventArgs(CollectionChange.Reset);
            public static VectorChangedEventArgs Reset
            {
                get { return _reset; }
            }

            public VectorChangedEventArgs(CollectionChange cc, int index = -1, object item = null)
            {
                _cc = cc;
                _index = (uint)index;
            }
            public CollectionChange CollectionChange
            {
                get { return _cc; }
            }
            public uint Index
            {
                get { return _index; }
            }
        }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** IC1CollectionView

        public bool CanFilter { get { return true; } }
        public Predicate<object> Filter
        {
            get { return _filter; }
            set
            {
                if (_filter != value)
                {
                    _filter = value;
                    Refresh();
                }
            }
        }
        public bool CanGroup { get { return false; } }
        public IList<object> GroupDescriptions { get { return null; } }
        public bool CanSort { get { return true; } }
        public IList<SortDescription> SortDescriptions { get { return _sort; } }
        public IEnumerable SourceCollection { get { return _source as IEnumerable; } }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** ICollectionView

        /// <summary>
        /// Occurs after the current item has changed.
        /// </summary>
        public event EventHandler<object> CurrentChanged;
        /// <summary>
        /// Occurs before the current item changes.
        /// </summary>
        public event CurrentChangingEventHandler CurrentChanging;
        /// <summary>
        /// Occurs when the view collection changes.
        /// </summary>
        public event VectorChangedEventHandler<object> VectorChanged;
        /// <summary>
        /// Gets a colletion of top level groups.
        /// </summary>
        public IObservableVector<object> CollectionGroups
        {
            get { return null; }
        }
        /// <summary>
        /// Gets the current item in the view.
        /// </summary>
        public object CurrentItem
        {
            get { return _index > -1 && _index < _view.Count ? _view[_index] : null; }
            set { MoveCurrentTo(value); }
        }
        /// <summary>
        /// Gets the ordinal position of the current item in the view.
        /// </summary>
        public int CurrentPosition { get { return _index; } }
        public bool IsCurrentAfterLast { get { return _index >= _view.Count; } }
        public bool IsCurrentBeforeFirst { get { return _index < 0; } }
        public bool MoveCurrentToFirst() { return MoveCurrentToIndex(0); }
        public bool MoveCurrentToLast() { return MoveCurrentToIndex(_view.Count - 1); }
        public bool MoveCurrentToNext() { return MoveCurrentToIndex(_index + 1); }
        public bool MoveCurrentToPosition(int index) { return MoveCurrentToIndex(index); }
        public bool MoveCurrentToPrevious() { return MoveCurrentToIndex(_index - 1); }
        public int IndexOf(object item) { return _view.IndexOf(item); }
        public bool MoveCurrentTo(object item) { return item != CurrentItem ? MoveCurrentToIndex(IndexOf(item)) : true; }

        // async operations not supported
        public bool HasMoreItems { get { return false; } }
        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            throw new NotSupportedException();
        }

        // list operations

        public bool IsReadOnly
        {
            get { return _sourceList == null || _sourceList.IsReadOnly; }
        }
        public void Add(object item)
        {
            CheckReadOnly();
            _sourceList.Add(item);
        }
        public void Insert(int index, object item)
        {
            CheckReadOnly();
            if (_sort.Count > 0 || _filter != null)
            {
                throw new Exception("Cannot insert items into sorted or filtered views.");
            }
            _sourceList.Insert(index, item);
        }
        public void RemoveAt(int index)
        {
            Remove(_view[index]);
        }
        public bool Remove(object item)
        {
            CheckReadOnly();
            _sourceList.Remove(item);
            return true;
        }
        public void Clear()
        {
            CheckReadOnly();
            _sourceList.Clear();
        }
        void CheckReadOnly()
        {
            if (IsReadOnly)
            {
                throw new Exception("The source collection cannot be modified.");
            }
        }
        public object this[int index]
        {
            get { return _view[index]; }
            set { _view[index] = value; }
        }
        public bool Contains(object item) { return _view.Contains(item); }
        public void CopyTo(object[] array, int arrayIndex) { _view.CopyTo(array, arrayIndex); }
        public int Count { get { return _view.Count; } }
        public IEnumerator<object> GetEnumerator() { return _view.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _view.GetEnumerator(); }

        #endregion

        //------------------------------------------------------------------------------------
        #region ** IEditableCollectionView

        object _addItem;
        object _editItem;

        public bool CanCancelEdit { get { return true; } }
        public object CurrentEditItem { get { return _editItem; } }
        public bool IsEditingItem { get { return _editItem != null; } }
        public void EditItem(object item)
        {
            var ieo = item as IEditableObject;
            if (ieo != null && ieo != _editItem)
            {
                ieo.BeginEdit();
            }
            _editItem = item;
        }
        public void CancelEdit()
        {
            var ieo = _editItem as IEditableObject;
            if (ieo != null)
            {
                ieo.CancelEdit();
            }
            _editItem = null;
        }
        public void CommitEdit()
        {
            if (_editItem != null)
            {
                // finish editing item
                var item = _editItem;
                var ieo = item as IEditableObject;
                if (ieo != null)
                {
                    ieo.EndEdit();
                }
                _editItem = null;

                // apply filter/sort after edits
                HandleItemChanged(item);
            }
        }

        public bool CanAddNew { get { return !IsReadOnly && _itemType != null; } }
        public object AddNew()
        {
            _addItem = null;
            if (_itemType != null)
            {
                _addItem = Activator.CreateInstance(_itemType);
                if (_addItem != null)
                {
                    this.Add(_addItem);
                }
            }
            return _addItem;
        }
        public void CancelNew()
        {
            if (_addItem != null)
            {
                this.Remove(_addItem);
                _addItem = null;
            }
        }
        public void CommitNew()
        {
            if (_addItem != null)
            {
                var item = _addItem;
                _addItem = null;
                HandleItemChanged(item);
            }
        }
        public bool CanRemove { get { return !IsReadOnly; } }
        public object CurrentAddItem { get { return _addItem; } }
        public bool IsAddingNew { get { return _addItem != null; } }
        #endregion

        //------------------------------------------------------------------------------------
        #region ** INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        #endregion

        //------------------------------------------------------------------------------------
        #region ** IComparer<object>

        int IComparer<object>.Compare(object x, object y)
        {
            // get property descriptors (once)
            if (_sortProps.Count == 0)
            {
                var typeInfo = x.GetType().GetTypeInfo();
                foreach (var sd in _sort)
                {
                    _sortProps[sd.PropertyName] = typeInfo.GetDeclaredProperty(sd.PropertyName);
                }
            }

            // compare two items
            foreach (var sd in _sort)
            {
                var pi = _sortProps[sd.PropertyName];
                var cx = pi.GetValue(x) as IComparable;
                var cy = pi.GetValue(y) as IComparable;

                try
                {
                    var cmp =
                        cx == cy ? 0 :
                        cx == null ? -1 :
                        cy == null ? +1 :
                        cx.CompareTo(cy);

                    if (cmp != 0)
                    {
                        return sd.Direction == ListSortDirection.Ascending ? +cmp : -cmp;
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("comparison failed...");
                }
            }
            return 0;
        }

        #endregion
    }
}
