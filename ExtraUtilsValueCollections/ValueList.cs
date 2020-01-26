using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NativeCollections.Utilites;

namespace ExtraUtils.ValueCollections
{
    /// <summary>
    /// Represents a stack-only collections of elements of the same type, and provides methods for add, remove and find values.
    /// <para>
    /// <see cref="ValueList{T}"/> make use of <see cref="ArrayPool{T}"/> to provides zero allocations.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of the elements.</typeparam>
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(ValueListDebugView<>))]
    public ref struct ValueList<T>
    {
        private T[]? _arrayFromPool;
        private Span<T> _span;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueList{T}" /> struct.
        /// </summary>
        /// <param name="initialBuffer">The initial buffer.</param>
        public ValueList(Span<T> initialBuffer)
        {
            if (initialBuffer.IsEmpty)
            {
                throw new ArgumentException("Buffer cannot be empty", nameof(initialBuffer));
            }

            _span = initialBuffer;
            _arrayFromPool = null;
            _count = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueList{T}" /> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public ValueList(int initialCapacity)
        {
            if(initialCapacity <= 0)
            {
                throw new ArgumentException("Initial capacity should be greater than 0");
            }

            _arrayFromPool = ArrayPool<T>.Shared.Rent(initialCapacity);
            _span = _arrayFromPool;
            _count = 0;
        }

        /// <summary>
        /// Creates a <see cref="ValueList{T}"/> from the given values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>A list from the given values.</returns>
        public static ValueList<T> CreateFrom(ReadOnlySpan<T> values)
        {
            if (values.IsEmpty)
            {
                return default;
            }

            int length = values.Length;
            ValueList<T> list = new ValueList<T>(length)
            {
                _count = length
            };
            values.CopyTo(list._span);
            return list;
        }

        /// <summary>
        /// Gets a value indicating if this list have elements.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets the number of elements in the list.
        /// </summary>
        public int Length => _count;

        /// <summary>
        /// Gets a number of elements this list can hold before resize.
        /// </summary>
        public int Capacity => _span.Length;

        /// <summary>
        /// Gets a a reference to the element in the given index.
        /// </summary>
        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index > _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
                }

                return ref _span[index];
            }
        }

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> view to the elements of this list.
        /// </summary>
        public ReadOnlySpan<T> Span => _span.Slice(0, _count);

        /// <summary>
        /// Adds the specified value to the list.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Add(T value)
        {
            if (_count == _span.Length)
            {
                Resize(_count * 2);
            }

            _span[_count++] = value;
        }

        /// <summary>
        /// Adds all the given values to the list.
        /// </summary>
        /// <param name="values">The values.</param>
        public void AddRange(ReadOnlySpan<T> values)
        {
            ResizeIfNeeded(values.Length);

            foreach (ref readonly var e in values)
            {
                _span[_count++] = e;
            }
        }

        /// <summary>
        /// Inserts the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public void Insert(int index, T value)
        {
            if (index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
            }

            if (_count == _span.Length)
            {
                Resize(_count * 2);
            }

            Span<T> src = _span.Slice(index, _count - index);
            Span<T> dst = _span.Slice(index + 1);
            src.CopyTo(dst);

            _span[index] = value;
            _count++;
        }

        /// <summary>
        /// Inserts all the given values at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="values">The values.</param>
        public void InsertRange(int index, ReadOnlySpan<T> values)
        {
            if (index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
            }

            int length = values.Length;
            ResizeIfNeeded(length);

            Span<T> src = _span.Slice(index, _count - index);
            Span<T> dst = _span.Slice(index + length);
            src.CopyTo(dst);
            values.CopyTo(_span.Slice(index));

            _count += length;
        }

        /// <summary>
        /// Removes the last element of the list.
        /// </summary>
        /// <returns>The last element.</returns>
        public T RemoveLast()
        {
            if (_count > 0)
            {
                T last = _span[_count - 1];
                RemoveAt(_count - 1);
                return last;
            }

            throw new InvalidOperationException("The list is empty");
        }

        /// <summary>
        /// Removes the given value from the list.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool Remove(T value)
        {
            int index = IndexOf(value);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
            }

            _count--;
            if (_count > index)
            {
                Span<T> src = _span.Slice(index + 1, _count - index);
                Span<T> dst = _span.Slice(index);
                src.CopyTo(dst);
            }

            _span[_count] = default!;
        }

        /// <summary>
        /// Clears the content of this list.
        /// </summary>
        public void Clear()
        {
            if (default(T)! == null)
            {
                _span.Slice(0, _count).Clear();
            }

            _count = 0;
        }

        /// <summary>
        /// Determines whether this list contains the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the list contains the value; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(T value)
        {
            return IndexOf(value) >= 0;
        }

        /// <summary>
        /// Gets the index of the first element that match the value or -1 if not found.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first match or -1 if not found.</returns>
        public int IndexOf(T value)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(value, _span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the index of the last element that match the specified or -1 if not found.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the last match or -1 if not found.</returns>
        public int LastIndexOf(T value)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = _count - 1; i >= 0; i--)
            {
                if (comparer.Equals(value, _span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates a new array with the elements of this list.
        /// </summary>
        /// <returns>A new array with this list elements.</returns>
        public T[] ToArray()
        {
            return _span.Slice(0, _count).ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="ValueArray{T}"/> with the elements of this list.
        /// </summary>
        /// <returns>A new array with this list elements.</returns>
        public ValueArray<T> ToValueArray()
        {
            ValueArray<T> array = new ValueArray<T>(_count);
            _span.Slice(0, _count).CopyTo(array._span);
            return array;
        }

        /// <summary>
        /// Creates a new <see cref="ValueArray{T}"/> with the elements of this list and then dispose this instance.
        /// </summary>
        /// <returns>A new array with this list elements.</returns>
        public ValueArray<T> ToValueArrayAndDispose()
        {
            ValueArray<T> array = new ValueArray<T>(_arrayFromPool!, _count);

            // Just invalidates this instance
            this = default;
            return array;
        }

        /// <summary>
        /// Gets an enumerator over the elements of this list.
        /// </summary>
        /// <returns>An enumerator over the elements of this list.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        /// <summary>
        /// Gets a string representation of the elements of this list.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this list elements.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            Enumerator enumerator = GetEnumerator();

            sb.Append('[');
            if (enumerator.MoveNext())
            {
                while (true)
                {
                    T current = enumerator.Current;
                    sb.Append(current!.ToString());

                    if (enumerator.MoveNext())
                    {
                        sb.Append(',');
                        sb.Append(' ');
                    }
                    else
                    {
                        break;
                    }
                }
            }

            sb.Append(']');
            return StringBuilderCache.ToStringAndRelease(ref sb!);
        }

        /// <summary>
        /// Releases the resources used for this list.
        /// </summary>
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                // Only clears the array if we hold references
                bool clearArray = default(T)! == null;
                ArrayPool<T>.Shared.Return(_arrayFromPool, clearArray);
            }

            this = default;
        }

        private void ResizeIfNeeded(int min)
        {
            int required = _count + min;
            if (required > _span.Length)
            {
                int newLength = _span.Length * 2;
                if (required > newLength)
                {
                    newLength = required;
                }

                Resize(newLength);
            }
        }

        private void Resize(int capacity)
        {
            Debug.Assert(capacity > 0);
            Debug.Assert(capacity > _span.Length && !_span.IsEmpty);

            T[] newArray = ArrayPool<T>.Shared.Rent(capacity);
            _span.CopyTo(newArray);

            if (_arrayFromPool != null)
            {
                // Only clears the array if we hold references
                bool clearArray = default(T)! == null;
                ArrayPool<T>.Shared.Return(_arrayFromPool, clearArray);
            }

            _span = _arrayFromPool = newArray;
        }

        /// <summary>
        /// An enumerator over the elements of a <see cref="ValueList{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private readonly int _length;
            private int _pos;

            internal Enumerator(ref ValueList<T> list)
            {
                _span = list._span;
                _length = list._count;
                _pos = -1;
            }

            /// <summary>
            /// Gets a reference to the current element.
            /// </summary>
            public ref T Current
            {
                get
                {
                    if (_pos < 0 || _pos > _length)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    return ref _span[_pos];
                }
            }

            /// <summary>
            /// Move the the next element.
            /// </summary>
            public bool MoveNext()
            {
                int i = _pos + 1;
                if (i < _length)
                {
                    _pos = i;
                    return true;
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
