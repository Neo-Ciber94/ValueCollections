using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using NativeCollections.Utilites;

namespace ExtraUtils.ValueCollections
{
    /// <summary>
    /// Represents a key-value store in a <see cref="ValueMap{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    public struct KeyValueEntry<TKey, TValue>
    {
        internal TKey key;
        internal TValue value;
        internal int hashCode;
        internal int bucket;
        internal int next;

        /// <summary>
        /// Gets the key.
        /// </summary>
        public TKey Key => key;

        /// <summary>
        /// Gets the value.
        /// </summary>
        public TValue Value => value;
    }

    /// <summary>
    /// Represents a stack-only collection of key-values.
    /// <para>
    /// <see cref="ValueMap{TKey, TValue}"/> make use of <see cref="ArrayPool{T}"/> to provides zero allocations.
    /// </para> 
    /// </summary>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values.</typeparam>
    public ref struct ValueMap<TKey, TValue>
    {
        internal enum InsertionMode
        {
            Any, Add, Replace
        }

        private KeyValueEntry<TKey, TValue>[]? _arrayFromPool;
        private Span<KeyValueEntry<TKey, TValue>> _entries;
        private int _count;
        private int _freeCount;
        private int _freeList;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueMap{TKey, TValue}" /> struct.
        /// </summary>
        /// <param name="initialBuffer">The initial buffer.</param>
        public ValueMap(Span<KeyValueEntry<TKey, TValue>> initialBuffer)
        {
            if (initialBuffer.IsEmpty)
            {
                throw new ArgumentException("Buffer cannot be empty", nameof(initialBuffer));
            }

            _entries = initialBuffer;
            _arrayFromPool = null;
            _count = 0;
            _freeCount = 0;
            _freeList = -1;

            Initializate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueMap{TKey, TValue}" /> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public ValueMap(int initialCapacity)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentException("Initial capacity should be greater than 0");
            }

            _arrayFromPool = ArrayPool<KeyValueEntry<TKey, TValue>>.Shared.Rent(initialCapacity);
            _entries = _arrayFromPool;
            _count = 0;
            _freeCount = 0;
            _freeList = -1;

            Initializate();
        }

        /// <summary>
        /// Determines if this map have elements.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets the number of elements in this map.
        /// </summary>
        public int Length => _count;

        /// <summary>
        /// Gets the number of elements this map can hold before resize.
        /// </summary>
        public int Capacity => _entries.Length;

        /// <summary>
        /// Gets or sets the <see cref="TValue" /> for the specified key specified key.
        /// </summary>
        /// <value>
        /// The <see cref="TValue" />.
        /// </value>
        /// <param name="key">The key associated to the value.</param>
        /// <returns>The value associated to the key.</returns>
        /// <exception cref="KeyNotFoundException">If the key don't exists when inserting the value.</exception>
        public TValue this[TKey key]
        {
            get
            {
                int index = FindEntry(key);
                if (index >= 0)
                {
                    return _entries[index].value;
                }

                throw new KeyNotFoundException($"Cannot find the key: '{key}'");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => TryInsert(key, value, InsertionMode.Any);
        }

        /// <summary>
        /// Adds the specified key and value to the map.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">If the key already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            if (!TryInsert(key, value, InsertionMode.Add))
            {
                throw new ArgumentException("Duplicated key");
            }
        }

        /// <summary>
        /// Attemps to add the specified key and value to the map.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if is added, otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(TKey key, TValue value)
        {
            return TryInsert(key, value, InsertionMode.Add);
        }

        /// <summary>
        /// Adds or update the specified key and value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOrUpdate(TKey key, TValue value)
        {
            TryInsert(key, value, InsertionMode.Any);
        }

        /// <summary>
        /// Replaces the value associated to the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns><c>true</c> if the value was removed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Replace(TKey key, TValue newValue)
        {
            return TryInsert(key, newValue, InsertionMode.Replace);
        }

        /// <summary>
        /// Removes key and the value associated to it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the key and value were removed.</returns>
        public bool Remove(TKey key)
        {
            if (_entries.IsEmpty)
            {
                return false;
            }

            var comparer = EqualityComparer<TKey>.Default;
            var hashCode = GetHashCode(key);
            var bucket = GetBucket(hashCode, _entries.Length);
            var index = _entries[bucket].bucket;
            int last = -1;

            while (index >= 0)
            {
                ref KeyValueEntry<TKey, TValue> entry = ref _entries[index];

                if (comparer.Equals(entry.key, key) && entry.hashCode == hashCode)
                {
                    if (last >= 0)
                    {
                        _entries[last].next = _entries[index].next;
                    }
                    else
                    {
                        _entries[bucket].bucket = _entries[bucket].next;
                    }

                    entry.next = _freeList;
                    entry.hashCode = -1;
                    _freeList = index;
                    _freeCount++;
                    return true;
                }

                last = index;
                index = entry.next;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the map contains the specified key key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>
        ///   <c>true</c> if the map contains the key; otherwise, <c>false</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }

        /// <summary>
        /// Determines whether the map contains the specified key value.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>
        ///   <c>true</c> if the map contains the value; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsValue(TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;

            for(int i = 0; i < _count; i++)
            {
                ref KeyValueEntry<TKey, TValue> entry = ref _entries[i];

                if(comparer.Equals(entry.value, value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a reference to the value associated to the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>A reference to the value.</returns>
        /// <exception cref="KeyNotFoundException">If the key is not found.</exception>
        public ref TValue GetValueReference(TKey key)
        {
            int index = FindEntry(key);
            if (index >= 0)
            {
                return ref _entries[index].value;
            }

            throw new KeyNotFoundException($"Cannot find the key: '{key}'");
        }

        /// <summary>
        /// Attemps to get the value associated to the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the key exists; otherwise false.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            int index = FindEntry(key);
            if (index >= 0)
            {
                value = _entries[index].value;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Attemps to get a reference to the value associated to the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">A reference to the value.</param>
        /// <returns><c>true</c> if the key exists; otherwise false.</returns>
        public bool TryGetValueReference(TKey key, out ByReference<TValue> value)
        {
            int index = FindEntry(key);
            if (index >= 0)
            {
                value = new ByReference<TValue>(ref _entries[index].value);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Clears the content of this map.
        /// </summary>
        public void Clear()
        {
            if (_entries.IsEmpty)
            {
                return;
            }

            if (typeof(TKey) == null || typeof(TValue) == null)
            {
                _entries.Clear();
            }

            _count = 0;
            _freeCount = 0;
            _freeList = -1;

            Initializate();
        }

        /// <summary>
        /// Releases the resouces used for this map.
        /// </summary>
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                bool clearArray = default(TKey)! == null || default(TValue)! == null;
                ArrayPool<KeyValueEntry<TKey, TValue>>.Shared.Return(_arrayFromPool, clearArray);
            }

            this = default;
        }

        /// <summary>
        /// Gets a string representation of the key-values of this map.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (_count == 0)
            {
                return "[]";
            }

            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append('[');

            Enumerator enumerator = GetEnumerator();

            if (enumerator.MoveNext())
            {
                while (true)
                {
                    ref KeyValuePair<TKey, TValue> pair = ref enumerator.Current;
                    sb.Append('{');
                    sb.Append(pair.Key!.ToString());
                    sb.Append(',');
                    sb.Append(' ');
                    sb.Append(pair.Value!.ToString());
                    sb.Append('}');

                    if (!enumerator.MoveNext())
                    {
                        break;
                    }

                    sb.Append(',');
                    sb.Append(' ');
                }
            }

            sb.Append(']');
            return StringBuilderCache.ToStringAndRelease(ref sb!);
        }

        /// <summary>
        /// Gets an enumerator over the elements of the map.
        /// </summary>
        /// <returns>An enumerator over the elements of the map.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        private void Initializate()
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                _entries[i].bucket = -1;
                _entries[i].hashCode = -1;
            }
        }

        private bool TryInsert(TKey key, TValue value, InsertionMode mode)
        {
            if(key == null)
            {
                throw new ArgumentException("key cannot be null");
            }

            if (_entries.IsEmpty)
            {
                return false;
            }

            var comparer = EqualityComparer<TKey>.Default;
            int hashCode = GetHashCode(key);
            int bucket = GetBucket(hashCode, _entries.Length);
            int index = _entries[bucket].bucket;
            int capacity = _entries.Length;

            while (index >= 0)
            {
                ref KeyValueEntry<TKey, TValue> entry = ref _entries[index];
                switch (mode)
                {
                    case InsertionMode.Any:
                    case InsertionMode.Replace:
                        if (comparer.Equals(entry.key, key) && hashCode == entry.hashCode)
                        {
                            entry.value = value;
                            return true;
                        }
                        break;
                    case InsertionMode.Add:
                        if (comparer.Equals(entry.key, key) && hashCode == entry.hashCode)
                            return false;
                        break;
                }

                index = entry.next;
            }

            if (mode == InsertionMode.Replace)
            {
                return false;
            }

            if (_freeCount > 0)
            {
                index = _freeList;
                _freeList = _entries[_freeList].next;
                _freeCount--;
            }
            else
            {
                if (_count == capacity)
                {
                    Resize(_count * 2);
                    bucket = GetBucket(hashCode, capacity);
                }

                index = _count;
                _count++;
            }

            _entries[index].key = key;
            _entries[index].value = value;
            _entries[index].hashCode = hashCode;
            _entries[index].next = _entries[bucket].bucket;
            _entries[bucket].bucket = index;
            return true;
        }

        private readonly int FindEntry(in TKey key)
        {
            if (_entries.IsEmpty || key == null)
                return -1;

            var comparer = EqualityComparer<TKey>.Default;
            var hashCode = GetHashCode(key);
            var bucket = GetBucket(hashCode, _entries.Length);
            int index = _entries[bucket].bucket;

            while (index >= 0)
            {
                ref KeyValueEntry<TKey, TValue> entry = ref _entries[index];
                if (comparer.Equals(entry.key, key) && entry.hashCode == hashCode)
                {
                    return index;
                }

                index = entry.next;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucket(int hashCode, int capacity)
        {
            return hashCode % capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHashCode(TKey key)
        {
            return key == null ? 0 : key.GetHashCode() & int.MaxValue;
        }

        private void Resize(int capacity)
        {
            Debug.Assert(capacity > 0);
            Debug.Assert(!_entries.IsEmpty);

            ArrayPool<KeyValueEntry<TKey, TValue>> pool = ArrayPool<KeyValueEntry<TKey, TValue>>.Shared;
            KeyValueEntry<TKey, TValue>[] newEntries = pool.Rent(capacity);
            _entries.CopyTo(newEntries);

            for (int i = 0; i < capacity; i++)
            {
                newEntries[i].bucket = -1;
            }

            for (int i = 0; i < _count; i++)
            {
                ref KeyValueEntry<TKey, TValue> entry = ref newEntries[i];

                if (entry.hashCode >= 0)
                {
                    int hashCode = GetHashCode(entry.key);
                    int bucket = GetBucket(hashCode, capacity);
                    newEntries[i].next = newEntries[bucket].bucket;
                    newEntries[bucket].bucket = i;
                }
            }

            if (_arrayFromPool != null)
            {
                bool clearArray = default(TKey)! == null || default(TValue)! == null;
                ArrayPool<KeyValueEntry<TKey, TValue>>.Shared.Return(_arrayFromPool, clearArray);
            }

            _entries = _arrayFromPool = newEntries;
        }

        /// <summary>
        /// An enumerator over the elements of a <see cref="ValueMap{TKey, TValue}"/>.
        /// </summary>
        unsafe public ref struct Enumerator
        {
            private readonly Span<KeyValueEntry<TKey, TValue>> _entries;
            private readonly int _length;
            private int _pos;

            internal Enumerator(ref ValueMap<TKey, TValue> map)
            {
                _entries = map._entries;
                _length = map._count;
                _pos = -1;
            }

            /// <summary>
            /// Gets the current value.
            /// </summary>
            public readonly ref KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (_pos < 0 || _pos > _length)
                    {
                        throw new InvalidOperationException("Invalid state");
                    }

                    ref KeyValueEntry<TKey, TValue> entry = ref _entries[_pos];
                    return ref Unsafe.As<KeyValueEntry<TKey, TValue>, KeyValuePair<TKey, TValue>>(ref entry);
                }
            }

            /// <summary>
            /// Moves to the next value.
            /// </summary>
            public bool MoveNext()
            {
                if (_length == 0)
                    return false;

                int i = _pos + 1;
                while (i < _length)
                {
                    ref KeyValueEntry<TKey, TValue> entry = ref _entries[_pos];
                    if (entry.hashCode >= 0)
                    {
                        _pos = i;
                        return true;
                    }

                    i++;
                }

                return false;
            }

            /// <summary>
            /// Resets this enumerator.
            /// </summary>
            public void Reset()
            {
                if (_length > 0)
                {
                    _pos = -1;
                }
            }

            /// <summary>
            /// Invalidates this enumerator.
            /// </summary>
            public void Dispose()
            {
                if (_length > 0)
                {
                    this = default;
                }
            }
        }
    }
}
