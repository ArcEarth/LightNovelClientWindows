//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;

namespace LightNovel.Common
{
	// This class can used as a jumpstart for implementing ISupportIncrementalLoading. 
	// Implementing the ISupportIncrementalLoading interfaces allows you to create a list that loads
	//  more data automatically when the user scrolls to the end of of a GridView or ListView.
	public abstract class IncrementalLoadingBase: IList, ISupportIncrementalLoading, INotifyCollectionChanged
	{
		#region IList

		public int Add(object value)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(object value)
		{
			Debug.WriteLine("Contains Get Called");
			return _storage.Contains(value);
		}

		public int IndexOf(object value)
		{
			Debug.WriteLine("IndexOf Get Called");
			return _storage.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public void Remove(object value)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public object this[int index]
		{
			get
			{
				if (index >= _storage.Count)
					Debug.WriteLine("Shit! Index out of range at : " + index);
				//Debug.WriteLine(String.Format("this[{0}]",index));
				return _storage[index];
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void CopyTo(Array array, int index)
		{
			//Debug.WriteLine("CopyTo Called");
			((IList)_storage).CopyTo(array, index);
		}

		public int Count
		{
			get {
				Debug.WriteLine("Count Get Called");
				return _storage.Count;
			}
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerator GetEnumerator()
		{
			Debug.WriteLine("GetEnumerator() Get Called");
			return _storage.GetEnumerator();
		}

		#endregion 
	
		#region ISupportIncrementalLoading

		public bool HasMoreItems
		{
			get {
				Debug.WriteLine("HasMoreItems Called");
				return HasMoreItemsOverride();
			}
		}

		public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			Debug.WriteLine("LoadMoreItemsAsync Raw API Called");
			if (_busy)
			{
				throw new InvalidOperationException("Only one operation in flight at a time");
			}

			_busy = true;

			return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
		}

		#endregion 

		#region INotifyCollectionChanged

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion 

		#region Private methods

		async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
		{
			try
			{
				var items = await LoadMoreItemsOverrideAsync(c, count);
				var baseIndex = _storage.Count;

				_storage.AddRange(items);

				// Now notify of the new items
				NotifyOfInsertedItems(baseIndex, items.Count);

				return new LoadMoreItemsResult { Count = (uint)items.Count };
			}
			finally
			{
				_busy = false;
			}
		}

		void NotifyOfInsertedItems(int baseIndex, int count)
		{
			if (CollectionChanged == null)
			{
				return;
			}

			for (int i = 0; i < count; i++)
			{
				var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, _storage[i + baseIndex], i + baseIndex);
				CollectionChanged(this, args);
			}
		}

		#endregion

		#region Overridable methods

		protected abstract Task<IList<object>> LoadMoreItemsOverrideAsync(CancellationToken c, uint count);
		protected abstract bool HasMoreItemsOverride();

		#endregion 

		#region State

		protected List<object> _storage = new List<object>();
		bool _busy = false;

		#endregion 
	}

	public class VectorEnumerator : IEnumerator
	{
		public VectorEnumerator(IList container, int startIndex = 0, int count = -1)
		{
			Container = container;
			CurrentIndex = startIndex;
			StartIndex = startIndex;
			if (count < 0)
				EndIndex = container.Count - startIndex;
			else
				EndIndex = startIndex + count;
		}
		IList Container;
		int CurrentIndex;
		int StartIndex;
		int EndIndex;
		public object Current
		{
			get
			{
				return Container[CurrentIndex];
			}
		}
		public bool MoveNext()
		{
			return ++CurrentIndex < EndIndex;
			
		}
		public void Reset()
		{
			CurrentIndex = StartIndex;
		}
	}



	public class PagelizedIncrementalVector<T> : INotifyCollectionChanged, IList, ISupportIncrementalLoading
	{
		class Page
		{
			public int EndIndex; // Contains the last_logical_address + 1 of each page
			public long LastAcessTime;
			public Task LoadingTask; // 
			public object[] Data { get; set; }
		}

		#region IList

		public int Add(object value)
		{
			throw new NotSupportedException();
		}

		public void Clear()
		{
			throw new NotSupportedException();
		}

		public bool Contains(object value)
		{
			throw new NotSupportedException("Object query is not supported");
			//return _storage.Contains(value);
		}

		public int IndexOf(object value)
		{
			throw new NotSupportedException("Object query is not supported");
			//return _storage.IndexOf(value);
		}

		public void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return true; }
		}

		public void Remove(object value)
		{
			throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotSupportedException();
		}

		public object this[int index]
		{
			get
			{
				// Address finding base on page table
				var page = CurrentPageNo;
				while (index <= Pages[page].EndIndex) // find the page
					--page;
				while (index >= Pages[page].EndIndex) // find the page
					++page;
				if (CurrentPageNo > 0)
					index -= Pages[CurrentPageNo - 1].EndIndex; // convert logical address to physical address in page
				if (Pages[page].Data == null) // If the page is not cached, than we need to load the page to cache
				{
					if (Pages[page].LoadingTask == null)
						Pages[page].LoadingTask = LoadPageAsync(page);
					if (!Pages[page].LoadingTask.IsCompleted)
					{
						Debug.WriteLine(String.Format("Page [{0}] is not expected, ad-hoc loading into cache", page));
						Pages[page].LoadingTask.Wait();
					}
					Pages[page].LoadingTask = null;
				}
				Pages[page].LastAcessTime = _internalTick++;
				return Pages[page].Data[index]; 
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public void CopyTo(Array array, int index)
		{
			throw new NotSupportedException("VirtualzedVectorCannot be copy to");
			//Debug.WriteLine("CopyTo Called");
			//((IList)_storage).CopyTo(array, index);
		}

		public int Count
		{
			get
			{
				return Pages[Pages.Count - 1].EndIndex;
			}
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerator GetEnumerator()
		{
			Debug.WriteLine("GetEnumerator() Get Called");
			return new VectorEnumerator(this);
		}

		#endregion

		#region ISupportIncrementalLoading

		public bool HasMoreItems
		{
			get
			{
				return true;
			}
		}

		public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			Debug.WriteLine(String.Format("LoadMoreItemsAsync[{0}] ...",count));
			if (_busy)
			{
				throw new InvalidOperationException("Only one operation in flight at a time");
			}
			_busy = true;

			return AsyncInfo.Run((c) => LoadMoreItemsAsync(c, count));
		}

		#endregion

		#region INotifyCollectionChanged

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		#region IOberservableVector
		//VectorChangedEventHandler<T> VectorChanged;
		#endregion

		#region Private methods

		async Task<LoadMoreItemsResult> LoadMoreItemsAsync(CancellationToken c, uint count)
		{
			try
			{
				int noBeforeLoad = Pages[CurrentPageNo].EndIndex;
				while (Pages[CurrentPageNo].EndIndex - noBeforeLoad < count && !c.IsCancellationRequested)
				{
					await LoadPageToCacheAsync(CurrentPageNo+1);

					int baseIdx = Pages[CurrentPageNo].EndIndex;
					int len = Pages[CurrentPageNo+1].Data.Length;
					for (int i = 0; i < len; i++)
					{
						var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, Pages[CurrentPageNo + 1].Data[i], i + baseIdx);
						CollectionChanged(this, args);
					}
					++CurrentPageNo;
				}
				var loadCount = (Pages[CurrentPageNo].EndIndex - noBeforeLoad);
				_busy = false;
				return new LoadMoreItemsResult { Count = (uint)loadCount };
			}
			finally
			{
				_busy = false;
			}
		}

		protected async Task LoadPageToCacheAsync(int page)
		{
			if (Pages[page].LoadingTask != null)
			{
				await Pages[page].LoadingTask;
				return;
			}
			Debug.WriteLine(String.Format("Loading Page [{0}] into cache...", page));
			Pages[page].Data = (await LoadPageAsync(page)).Cast<object>().ToArray();
			if (page > 0)
				Pages[page].EndIndex = Pages[page - 1].EndIndex + Pages[page].Data.Length;
			++PagesInCache;
			if (PagesInCache > 3)
			{
				// Remove oldest cache ?! bad performance...
				long min = Pages.Min(p => p.LastAcessTime);
				Pages.RemoveAll(p => p.LastAcessTime == min);
				Debug.WriteLine(String.Format("Page [{0}] is replaced out from cache", min));
			}
		}
		#endregion


		#region State

		long _internalTick; // For latest access counting
		int CurrentPageNo;
		int PagesInCache;
		List<Page> Pages; 
		bool _busy = false;


		public delegate Task<IEnumerable<T>> LoadPageAsyncDelegate(int page);

		public LoadPageAsyncDelegate LoadPageAsync { get; set; }
		#endregion
	}

}
