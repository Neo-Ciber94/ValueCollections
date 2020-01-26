using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NativeCollections.Utilites;

namespace ExtraUtils.ValueCollections
{
    /// <summary>
    /// Represents a stack-only FIFO (first-in first-out) collection of elements.
    /// <para>
    /// <see cref="ValueStack{T}"/> make use of <see cref="ArrayPool{T}"/> to provides zero allocations.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of the elements.</typeparam>
    public ref struct ValueStack<T>
    {
        private T[]? _arrayFromPool;
        private Span<T> _span;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueStack{T}" /> struct.
        /// </summary>
        /// <param name="initialBuffer">The initial buffer.</param>
        public ValueStack(Span<T> initialBuffer)
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
        /// Initializes a new instance of the <see cref="ValueStack{T}" /> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public ValueStack(int initialCapacity)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentException("Initial capacity should be greater than 0");
            }

            _arrayFromPool = ArrayPool<T>.Shared.Rent(initialCapacity);
            _span = _arrayFromPool;
            _count = 0;
        }

        /// <summary>
        /// Gets a value indicating if this stack have elements.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets the number of elements in the stack.
        /// </summary>
        public int Length => _count;

        /// <summary>
        /// Gets a number of elements this stack can hold before resize.
        /// </summary>
        public int Capacity => _span.Length;

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> view to the elements of this stack.
        /// </summary>
        public ReadOnlySpan<T> Span => _span.Slice(0, _count);

        /// <summary>
        /// Adds the specified value at the top of the stack.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Push(T value)
        {
            if (_count == _span.Length)
            {
                Resize(_count * 2);
            }

            _span[_count++] = value;
        }

        /// <summary>
        /// Gets the value at the top of the stack.
        /// </summary>
        /// <returns>The top value of the stack.</returns>
        /// <exception cref="InvalidOperationException">If the stack is empty.</exception>
        public T Peek()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The stack is empty");
            }

            return _span[_count - 1];
        }

        /// <summary>
        /// Gets and removes the value at the top of the stack.
        /// </summary>
        /// <returns>The top value of the stack.</returns>
        /// <exception cref="InvalidOperationException">If the stack is empty.</exception>
        public T Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The stack is empty");
            }

            _count--;
            T value = _span[_count];
            _span[_count] = default!;
            return value;
        }

        /// <summary>
        /// Attemps to get the value at the top of the stack.
        /// </summary>
        /// <param name="value">The value at the top of the stack.</param>
        /// <returns><c>true</c> if there is a value; otherwise false.</returns>
        public bool TryPeek(out T value)
        {
            if (_count == 0)
            {
                value = default!;
                return false;
            }

            value = _span[_count - 1];
            return true;
        }

        /// <summary>
        /// Attemps to get and remove the value at the top of the stack.
        /// </summary>
        /// <param name="value">The value at the top of the stack.</param>
        /// <returns><c>true</c> if there is a value; otherwise false.</returns>
        public bool TryPop(out T value)
        {
            if (_count == 0)
            {
                value = default!;
                return false;
            }

            _count--;
            value = _span[_count];
            _span[_count] = default!;
            return true;
        }

        /// <summary>
        /// Gets a reference to the value at the top of the stack.
        /// </summary>
        /// <returns>A reference to the value at the top of the stack.</returns>
        /// <exception cref="InvalidOperationException">If the stack is empty.</exception>
        public ref T PeekReference()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The stack is empty");
            }

            return ref _span[_count - 1];
        }

        /// <summary>
        /// Attemps to get a reference to the value at the top of the stack.
        /// </summary>
        /// <param name="value">A reference to the value at the top of the stack.</param>
        /// <returns><c>true</c> if there is a value; otherwise false.</returns>
        public bool TryPeek(out ByReference<T> value)
        {
            if (_count == 0)
            {
                value = default!;
                return false;
            }

            value = new ByReference<T>(ref _span[_count - 1]);
            return true;
        }

        /// <summary>
        /// Clears the content of this stack.
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
        /// Determines whether this stack contains the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the stack contains the value; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(T value)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(value, _span[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new array with the elements of this stack.
        /// </summary>
        /// <returns>A new array with this stack elements.</returns>
        public T[] ToArray()
        {
            return _span.Slice(0, _count).ToArray();
        }

        /// <summary>
        /// Creates a new <see cref="ValueArray{T}"/> with the elements of this stack.
        /// </summary>
        /// <returns>A new array with this stack elements.</returns>
        public ValueArray<T> ToValueArray()
        {
            ValueArray<T> array = new ValueArray<T>(_count);
            _span.Slice(0, _count).CopyTo(array._span);
            return array;
        }

        /// <summary>
        /// Creates a new <see cref="ValueArray{T}"/> with the elements of this stack and then dispose this instance.
        /// </summary>
        /// <returns>A new array with this stack elements.</returns>
        public ValueArray<T> ToValueArrayAndDispose()
        {
            ValueArray<T> array = new ValueArray<T>(_arrayFromPool!, _count);

            // Just invalidates this instance
            this = default;
            return array;
        }

        /// <summary>
        /// Releases the resources used for this stack.
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

        /// <summary>
        /// Gets an enumerator over the elements of this stack.
        /// </summary>
        /// <returns>An enumerator over the elements of this stack.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        /// <summary>
        /// Gets a string representation of the elements of this stack.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this stack elements.
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
        /// An enumerator over the elements of a <see cref="ValueStack{T}"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private readonly int _length;
            private int _pos;

            internal Enumerator(ref ValueStack<T> stack)
            {
                _span = stack._span;
                _length = stack._count;
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
