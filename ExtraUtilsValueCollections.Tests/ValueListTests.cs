using Xunit;
using ExtraUtils.ValueCollections;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtraUtils.ValueCollections.Tests
{
    public class ValueListTests
    {
        [Fact()]
        public void ValueListTest()
        {
            using ValueList<int> list = new ValueList<int>(stackalloc int[4]);
            Assert.Equal(0, list.Length);
            Assert.Equal(4, list.Capacity);
            Assert.True(list.IsEmpty);
        }

        [Fact()]
        public void ValueListTest1()
        {
            using ValueList<int> list = new ValueList<int>(6);
            Assert.Equal(0, list.Length);
            Assert.True(list.IsEmpty);
        }

        [Fact()]
        public void CreateFromTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4, 5 });
            Assert.Equal(5, list.Length);
            Assert.False(list.IsEmpty);

            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);
            Assert.Equal(4, list[3]);
            Assert.Equal(5, list[4]);
        }

        [Fact()]
        public void SpanTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4 });
            ReadOnlySpan<int> span = list.Span;

            Assert.Equal(4, span.Length);
            Assert.Equal(1, span[0]);
            Assert.Equal(2, span[1]);
            Assert.Equal(3, span[2]);
            Assert.Equal(4, span[3]);

            list.Clear();
            span = list.Span;
            Assert.Equal(0, span.Length);
        }

        [Fact()]
        public void AddTest()
        {
            using ValueList<int> list = new ValueList<int>(stackalloc int[4]);
            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);

            {
                Assert.Equal(1, list[0]);
                Assert.Equal(2, list[1]);
                Assert.Equal(3, list[2]);
                Assert.Equal(4, list[3]);
            }

            Assert.Equal(4, list.Length);
            Assert.False(list.IsEmpty);

            list.Add(5);
            list.Add(6);

            {
                Assert.Equal(1, list[0]);
                Assert.Equal(2, list[1]);
                Assert.Equal(3, list[2]);
                Assert.Equal(4, list[3]);
                Assert.Equal(5, list[4]);
                Assert.Equal(6, list[5]);
            }

            Assert.Equal(6, list.Length);
            Assert.False(list.IsEmpty);
        }

        [Fact()]
        public void AddRangeTest()
        {
            using ValueList<int> list = new ValueList<int>(stackalloc int[4]);
            list.AddRange(stackalloc int[] { 1, 2, 3, 4 });

            {
                Assert.Equal(1, list[0]);
                Assert.Equal(2, list[1]);
                Assert.Equal(3, list[2]);
                Assert.Equal(4, list[3]);
            }

            Assert.Equal(4, list.Length);
            Assert.False(list.IsEmpty);

            list.AddRange(stackalloc int[] { 5, 6 });

            {
                Assert.Equal(1, list[0]);
                Assert.Equal(2, list[1]);
                Assert.Equal(3, list[2]);
                Assert.Equal(4, list[3]);
                Assert.Equal(5, list[4]);
                Assert.Equal(6, list[5]);
            }

            Assert.Equal(6, list.Length);
            Assert.False(list.IsEmpty);
        }

        [Fact()]
        public void InsertTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 5 });
            list.Insert(3, 4);
            list.Insert(0, 0);

            Assert.Equal(6, list.Length);
            Assert.False(list.IsEmpty);

            Assert.Equal(0, list[0]);
            Assert.Equal(1, list[1]);
            Assert.Equal(2, list[2]);
            Assert.Equal(3, list[3]);
            Assert.Equal(4, list[4]);
            Assert.Equal(5, list[5]);
        }

        [Fact()]
        public void InsertRangeTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] {  2, 3 });
            list.InsertRange(2, stackalloc int[] { 4, 5 });
            list.InsertRange(0, stackalloc int[] { 0, 1 });

            Assert.Equal(6, list.Length);
            Assert.False(list.IsEmpty);
        }

        [Fact()]
        public void RemoveLastTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4, 5 });

            Assert.Equal(5, list.RemoveLast());
            Assert.Equal(4, list.RemoveLast());
            Assert.Equal(3, list.RemoveLast());
            Assert.Equal(2, list.RemoveLast());
            Assert.Equal(1, list.RemoveLast());
            Assert.True(list.IsEmpty);

            Exception? exception = null;

            try
            {
                list.RemoveLast();
            }
            catch (InvalidOperationException e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
        }

        [Fact()]
        public void RemoveTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3 });

            Assert.True(list.Remove(3));
            Assert.Equal(2, list.Length);

            Assert.True(list.Remove(2));
            Assert.Equal(1, list.Length);

            Assert.True(list.Remove(1));
            Assert.Equal(0, list.Length);

            Assert.False(list.Remove(3));
        }

        [Fact()]
        public void RemoveAtTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4, 5 });

            list.RemoveAt(2);
            Assert.Equal(new int[] { 1, 2, 4, 5 }, list.ToArray());

            list.RemoveAt(0);
            Assert.Equal(new int[] { 2, 4, 5 }, list.ToArray());

            list.RemoveAt(2);
            Assert.Equal(new int[] { 2, 4 }, list.ToArray());

            list.RemoveAt(1);
            Assert.Equal(new int[] { 2 }, list.ToArray());

            list.RemoveAt(0);
            Assert.Equal(Array.Empty<int>(), list.ToArray());

            Exception? exception = null;

            try
            {
                list.RemoveAt(1);
            }
            catch(ArgumentOutOfRangeException e)
            {
                exception = e;
            }

            Assert.NotNull(exception);
        }

        [Fact()]
        public void ClearTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4 });
            list.Clear();

            Assert.Equal(0, list.Length);
        }

        [Fact()]
        public void IndexOfTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4 });
            Assert.Equal(0, list.IndexOf(1));
            Assert.Equal(1, list.IndexOf(2));
            Assert.Equal(2, list.IndexOf(3));
            Assert.Equal(3, list.IndexOf(4));

            Assert.Equal(-1, list.IndexOf(0));
            Assert.Equal(-1, list.IndexOf(5));
        }

        [Fact()]
        public void LastIndexOfTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3, 4 });
            Assert.Equal(0, list.LastIndexOf(1));
            Assert.Equal(1, list.LastIndexOf(2));
            Assert.Equal(2, list.LastIndexOf(3));
            Assert.Equal(3, list.LastIndexOf(4));

            Assert.Equal(-1, list.LastIndexOf(0));
            Assert.Equal(-1, list.LastIndexOf(5));
        }

        [Fact()]
        public void ToArrayTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3 });
            Assert.Equal(new int[] { 1, 2, 3 }, list.ToArray());
        }

        [Fact()]
        public void GetEnumeratorTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3 });

            var enumerator = list.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(3, enumerator.Current);

            Assert.False(enumerator.MoveNext());
        }

        [Fact()]
        public void ToStringTest()
        {
            using ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3 });
            Assert.Equal("[1, 2, 3]", list.ToString());

            list.Clear();
            Assert.Equal("[]", list.ToString());
        }

        [Fact()]
        public void DisposeTest()
        {
            ValueList<int> list = ValueList<int>.CreateFrom(stackalloc int[] { 1, 2, 3 });
            list.Dispose();

            Assert.Equal(0, list.Length);
            Assert.Equal(0, list.Capacity);
            Assert.True(list.IsEmpty);
        }
    }
}