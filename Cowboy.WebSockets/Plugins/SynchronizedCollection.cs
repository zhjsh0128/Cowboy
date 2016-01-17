﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Cowboy.WebSockets.Plugins
{
    public class SynchronizedCollection<T> : IList<T>, IList
    {
        List<T> _items;
        object _sync;

        public SynchronizedCollection()
        {
            _items = new List<T>();
            _sync = new Object();
        }

        public SynchronizedCollection(object syncRoot)
        {
            if (syncRoot == null)
                throw new ArgumentNullException("syncRoot");

            _items = new List<T>();
            _sync = syncRoot;
        }

        public SynchronizedCollection(object syncRoot, IEnumerable<T> list)
        {
            if (syncRoot == null)
                throw new ArgumentNullException("syncRoot");
            if (list == null)
                throw new ArgumentNullException("list");

            _items = new List<T>(list);
            _sync = syncRoot;
        }

        public SynchronizedCollection(object syncRoot, params T[] list)
        {
            if (syncRoot == null)
                throw new ArgumentNullException("syncRoot");
            if (list == null)
                throw new ArgumentNullException("list");

            _items = new List<T>(list.Length);
            for (int i = 0; i < list.Length; i++)
                _items.Add(list[i]);

            _sync = syncRoot;
        }

        public int Count
        {
            get { lock (_sync) { return _items.Count; } }
        }

        protected List<T> Items
        {
            get { return _items; }
        }

        public object SyncRoot
        {
            get { return _sync; }
        }

        public T this[int index]
        {
            get
            {
                lock (_sync)
                {
                    return _items[index];
                }
            }
            set
            {
                lock (_sync)
                {
                    if (index < 0 || index >= _items.Count)
                        throw new ArgumentOutOfRangeException("index");

                    this.SetItem(index, value);
                }
            }
        }

        public void Add(T item)
        {
            lock (_sync)
            {
                int index = _items.Count;
                this.InsertItem(index, item);
            }
        }

        public void Clear()
        {
            lock (_sync)
            {
                this.ClearItems();
            }
        }

        public void CopyTo(T[] array, int index)
        {
            lock (_sync)
            {
                _items.CopyTo(array, index);
            }
        }

        public bool Contains(T item)
        {
            lock (_sync)
            {
                return _items.Contains(item);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_sync)
            {
                return _items.GetEnumerator();
            }
        }

        public int IndexOf(T item)
        {
            lock (_sync)
            {
                return this.InternalIndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_sync)
            {
                if (index < 0 || index > _items.Count)
                    throw new ArgumentOutOfRangeException("index");

                this.InsertItem(index, item);
            }
        }

        int InternalIndexOf(T item)
        {
            int count = _items.Count;

            for (int i = 0; i < count; i++)
            {
                if (object.Equals(_items[i], item))
                {
                    return i;
                }
            }
            return -1;
        }

        public bool Remove(T item)
        {
            lock (_sync)
            {
                int index = this.InternalIndexOf(item);
                if (index < 0)
                    return false;

                this.RemoveItem(index);
                return true;
            }
        }

        public void RemoveAt(int index)
        {
            lock (_sync)
            {
                if (index < 0 || index >= _items.Count)
                    throw new ArgumentOutOfRangeException("index");


                this.RemoveItem(index);
            }
        }

        protected virtual void ClearItems()
        {
            _items.Clear();
        }

        protected virtual void InsertItem(int index, T item)
        {
            _items.Insert(index, item);
        }

        protected virtual void RemoveItem(int index)
        {
            _items.RemoveAt(index);
        }

        protected virtual void SetItem(int index, T item)
        {
            _items[index] = item;
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList)_items).GetEnumerator();
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        object ICollection.SyncRoot
        {
            get { return _sync; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            lock (_sync)
            {
                ((IList)_items).CopyTo(array, index);
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                VerifyValueType(value);
                this[index] = (T)value;
            }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        int IList.Add(object value)
        {
            VerifyValueType(value);

            lock (_sync)
            {
                this.Add((T)value);
                return this.Count - 1;
            }
        }

        bool IList.Contains(object value)
        {
            VerifyValueType(value);
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            VerifyValueType(value);
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            VerifyValueType(value);
            this.Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            VerifyValueType(value);
            this.Remove((T)value);
        }

        static void VerifyValueType(object value)
        {
            if (value == null)
            {
                if (typeof(T).IsValueType)
                {
                    throw new ArgumentException("Invalid type.");
                }
            }
            else if (!(value is T))
            {
                throw new ArgumentException("Invalid type.");
            }
        }
    }
}