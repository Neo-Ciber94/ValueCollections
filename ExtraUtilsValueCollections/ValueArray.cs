using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using NativeCollections.Utilites;

namespace ExtraUtils.ValueCollections
{
    /// <summary>
    /// Represents a stack-only container with a fixed size.
    /// <para>
    /// <see cref="ValueArray{T}"/> make use of <see cref="ArrayPool{T}"/> to provides 0-allocations.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of the elements.</typeparam>
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(ValueArrayDebugView<>))]
    public ref struct ValueArray<T>
    {
        internal readonly T[]? _arrayFromPool;
        internal readonly Span<T> _span;
        internal readonly int _length;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueArray{T}" /> struct.
        /// </summary>
        /// <param name="initialBuffer">The initial buffer.</param>
        public ValueArray(Span<T> initialBuffer)
        {
            if (initialBuffer.IsEmpty)
            {
                throw new ArgumentException("Buffer cannot be empty", nameof(initialBuffer));
            }

            _span = initialBuffer;
            _arrayFromPool = null;
            _length = initialBuffer.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueArray{T}" /> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public ValueArray(int initialCapacity)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentException("Initial capacity should be greater than 0");
            }

            _arrayFromPool = ArrayPool<T>.Shared.Rent(initialCapacity);
            _span = _arrayFromPool;
            _length = initialCapacity;
        }

        internal ValueArray(T[] arrayFromPool, int length)
        {
            _arrayFromPool = arrayFromPool;
            _span = arrayFromPool;
            _length = length;
        }

        /// <summary>
        /// Gets a value indicating if this array is empty.
        /// </summary>
        public bool IsEmpty => _span.IsEmpty;

        /// <summary>
        /// Gets the capacity of this array.
        /// </summary>
        public int Length => _length;

        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index > _length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index.ToString());
                }

                return ref _span[index];
            }
        }

        /// <summary>
        /// Fills the this array with the specified content.
        /// </summary>
        public ReadOnlySpan<T> Span => _span.Slice(0, _length);

        /// <summary>
        /// Fills the this array with the specified content.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(T value) => _span.Slice(0, _length).Fill(value);

        /// <summary>
        /// Determines whether this array contains the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the array contains the value; otherwise, <c>false</c>.
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
            for (int i = 0; i < _length; i++)
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
            for (int i = _length - 1; i >= 0; i--)
            {
                if (comparer.Equals(value, _span[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Creates a new array with the elements of this instance.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray() => _span.Slice(0, _length).ToArray();

        /// <summary>
        /// Gets an enumerator over the elements of this instance.
        /// </summary>
        /// <returns>An enumerator over the elements of this instance.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        /// <summary>
        /// Gets a string representation of the elements of this list.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance elements.
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

        public static implicit operator ValueArray<T>(Span<T> span)
        {
            return new ValueArray<T>(span);
        }

        public static implicit operator ValueArray<T>(T[] array)
        {
            return new ValueArray<T>(array);
        }

        /// <summary>
        /// An enumerator over the elements of a <see cref="ValueArray{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private readonly int _length;
            private int _pos;

            internal Enumerator(ref ValueArray<T> array)
            {
                _span = array._span;
                _length = array._length;
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
