using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ExtraUtils.Utilities
{
    /// <summary>
    /// Represents a value type builder for strings.
    /// </summary>
    [DebuggerDisplay("{ToString(),raw}")]
    public ref struct ValueStringBuilder
    {
        private const char NewLine = '\n';

        private char[]? _arrayFromPool;
        private Span<char> _span;
        private int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueStringBuilder" /> struct.
        /// </summary>
        /// <param name="initialBuffer">The initial buffer.</param>
        public ValueStringBuilder(Span<char> initialBuffer)
        {
            if (initialBuffer.Length == 0)
            {
                throw new ArgumentException("The initialBuffer cannot be empty");
            }

            _span = initialBuffer;
            _arrayFromPool = null;
            _count = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueStringBuilder" /> struct.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity.</param>
        public ValueStringBuilder(int initialCapacity)
        {
            if (initialCapacity <= 0)
            {
                throw new ArgumentException("initialCapacity cannot be negative or zero");
            }

            _arrayFromPool = ArrayPool<char>.Shared.Rent(initialCapacity);
            _span = _arrayFromPool;
            _count = 0;
        }

        /// <summary>
        /// Creates a <see cref="ValueStringBuilder"/> using the values of the givan <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <returns>A new builder with the given values.</returns>
        public static ValueStringBuilder CreateFrom(in ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
            {
                return default;
            }

            char[] buffer = ArrayPool<char>.Shared.Rent(span.Length);
            ValueStringBuilder builder = new ValueStringBuilder(buffer);
            span.CopyTo(builder._span);
            builder._arrayFromPool = buffer;
            builder._count = span.Length;
            return builder;
        }

        /// <summary>
        /// Gets the number of elements in the builder.
        /// </summary>
        public int Length => _count;

        /// <summary>
        /// Gets the capacity of the builder.
        /// </summary>
        public int Capacity => _span.Length;

        /// <summary>
        /// Gets a value indicating if this instance is empty.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets the <see cref="char"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="char" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>A value at the specified index.</returns>
        public char this[int index]
        {
            get
            {
                if (index < 0 || index > _span.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), $"index: {index}");
                }

                return _span[index];
            }
        }

        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> to the elements of this builder.
        /// </summary>
        public ReadOnlySpan<char> Span => _span.Slice(0, _count);

        /// <summary>
        /// Appends the specified <see langword="char""/> to the builder.
        /// </summary>
        /// <param name="c">The value.</param>
        public void Append(char c)
        {
            if (_count == _span.Length)
            {
                Resize(_span.Length * 2);
            }

            _span[_count++] = c;
        }

        /// <summary>
        /// Appends the specified <see langword="char"/> the given number of times.
        /// </summary>
        /// <param name="c">The vlaue.</param>
        /// <param name="repeat">The number of times to repreat the char.</param>
        public void Append(char c, int repeat)
        {
            Debug.Assert(repeat >= 0);

            if (repeat <= 0)
            {
                return;
            }

            ResizeIfNeeded(repeat);
            while (repeat-- > 0)
            {
                _span[_count++] = c;
            }
        }

        /// <summary>
        /// Appends the specified value to the builder.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Append<T>(T value)
        {
            if(value != null)
            {
                string str = value.ToString()!;
                Append(str.AsSpan());
            }
        }

        /// <summary>
        /// Appends the specified <see cref="ReadOnlySpan{T}"/> to the builder.
        /// </summary>
        /// <param name="value">The value.</param>
        public void Append(in ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            ResizeIfNeeded(value.Length);

            unsafe
            {
                fixed (char* src = &Unsafe.AsRef(value.GetPinnableReference()), dst = &Unsafe.AsRef(_span.GetPinnableReference()))
                {
                    Unsafe.CopyBlock(dst + _count, src, (uint)(value.Length * sizeof(char)));
                    _count += value.Length;
                }
            }
        }

        /// <summary>
        /// Appends a new line.
        /// </summary>
        public void AppendLine()
        {
            if (_count == _span.Length)
            {
                Resize(_span.Length * 2);
            }

            _span[_count++] = NewLine;
        }

        /// <summary>
        /// Appends the specified <see langword="char""/> to the builder, followed by a new line.
        /// </summary>
        /// <param name="c">The value.</param>
        public void AppendLine(char c)
        {
            ResizeIfNeeded(2);
            _span[_count++] = c;
            _span[_count++] = NewLine;
        }

        /// <summary>
        /// Appends the value to the builder, followed by a new line.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLine<T>(T value)
        {
            if(value != null)
            {
                string str = value.ToString()!;
                AppendLine(str.AsSpan());
            }
        }

        /// <summary>
        /// Appends the specified <see cref="ReadOnlySpan{T}"/> to the builder, followed by a new line.
        /// </summary>
        /// <param name="value">The value.</param>
        public void AppendLine(in ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            int length = value.Length;
            ResizeIfNeeded(length + 1);

            unsafe
            {
                fixed (char* src = &Unsafe.AsRef(value.GetPinnableReference()), dst = &Unsafe.AsRef(_span.GetPinnableReference()))
                {
                    Unsafe.CopyBlock(dst + _count, src, (uint)(length * sizeof(char)));
                    _count += length;
                    _span[_count++] = NewLine;
                }
            }
        }

        /// <summary>
        /// Inserts the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="c">The value.</param>
        public void Insert(int index, char c)
        {
            if (index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"index: {index}");
            }

            if(_count == _span.Length)
            {
                Resize(_span.Length * 2);
            }

            Span<char> src = _span.Slice(index, _count - index);
            Span<char> dst = _span.Slice(index + 1);
            src.CopyTo(dst);
            _span[index] = c;
            _count++;
        }

        /// <summary>
        /// Inserts the value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public void Insert<T>(int index, T value)
        {
            if(value == null)
            {
                return;
            }

            string str = value.ToString()!;
            Insert(index, str.AsSpan());
        }

        /// <summary>
        /// Inserts the specified value at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public void Insert(int index, in ReadOnlySpan<char> value)
        {
            if(index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"index: {index}");
            }

            if (value.IsEmpty)
            {
                return;
            }

            int length = value.Length;
            ResizeIfNeeded(length);

            Span<char> src = _span.Slice(index, _count - index);
            Span<char> dest = _span.Slice(index + length);
            src.CopyTo(dest);
            value.CopyTo(_span.Slice(index));
            _count += length;
        }

        /// <summary>
        /// Clears the content of this builder.
        /// </summary>
        public void Clear()
        {
            if (_count == 0)
            {
                return;
            }

            _count = 0;
        }

        /// <summary>
        /// Gets the string builded by this instance.
        /// </summary>
        /// <returns>
        /// The <see cref="string" /> created by this instance.
        /// </returns>
        public override string ToString()
        {
            if (_count > 0)
            {
                return new string(_span.Slice(0, _count));
            }

            return string.Empty;
        }

        /// <summary>
        /// Releases the resouces used by this instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (_arrayFromPool != null)
            {
                ArrayPool<char>.Shared.Return(_arrayFromPool);
            }

            this = default;
        }

        /// <summary>
        /// Gets the string builded by this instance and then releases the resources of this instance.
        /// </summary>
        /// <returns>
        /// The <see cref="string" /> created by this instance.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToStringAndDispose()
        {
            string value = ToString();
            Dispose();
            return value;
        }

        /// <summary>
        /// Ensures this builder can hold the given of items without resize.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity)
        {
            if (capacity > _span.Length)
            {
                Resize(capacity);
            }
        }

        /// <summary>
        /// Gets an enumerator over the chars of this builder.
        /// </summary>
        /// <returns>An enumerator over the chars of this builder.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResizeIfNeeded(int charCount)
        {
            int required = _count + charCount;

            if (required > _span.Length)
            {
                int newLength = Math.Max(_span.Length * 2, required);
                Resize(newLength);
            }
        }

        private void Resize(int newCapacity)
        {
            Debug.Assert(newCapacity > _count);
            Debug.Assert(newCapacity > _span.Length);
            Debug.Assert(!_span.IsEmpty, "ValueStringBuilder is not initializated");

            char[] newArray = ArrayPool<char>.Shared.Rent(newCapacity);
            _span.CopyTo(newArray);
            _span = _arrayFromPool = newArray;;
        }

        /// <summary>
        /// An enumerator over the elements of a <see cref="ValueStringBuilder"/>.
        /// </summary>
        public ref struct Enumerator
        {
            private readonly Span<char> _span;
            private readonly int _lenght;
            private int _pos;

            internal Enumerator(ref ValueStringBuilder builder)
            {
                _span = builder._span;
                _lenght = builder._count;
                _pos = -1;
            }

            /// <summary>
            /// Gets the current element.
            /// </summary>
            public char Current
            {
                get
                {
                    if (_pos < 0 || _pos > _lenght)
                    {
                        throw new InvalidOperationException("Invalid enumerator state");
                    }

                    return _span[_pos];
                }
            }

            /// <summary>
            /// Move to the next element.
            /// </summary>
            public bool MoveNext()
            {
                int i = _pos + 1;
                if (i < _lenght)
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
                if (_span.Length > 0)
                {
                    _pos = -1;
                }
            }

            /// <summary>
            /// Release the resouces of this enumerator.
            /// </summary>
            public void Dispose()
            {
                this = default;
            }
        }
    }
}
