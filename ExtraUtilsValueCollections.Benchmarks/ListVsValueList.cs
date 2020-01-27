using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace ExtraUtils.ValueCollections.Benchmarks
{
    [MemoryDiagnoser]
    [MinColumn, MaxColumn]
    public class ListVsValueListBenchmarks
    {
        [Params(10, 100, 1000, 10_000, 100_000)]
        public int Size;

        [Benchmark(Baseline = true)]
        public void List()
        {
            List<int> list = new List<int>();
            for (int i = 0; i < Size; i++)
            {
                list.Add(i);
            }
        }

        [Benchmark]
        public void ListWithInitialCapacity()
        {
            List<int> list = new List<int>(Size);
            for (int i = 0; i < Size; i++)
            {
                list.Add(i);
            }
        }

        [Benchmark]
        public void ValueListWithInitialBuffer()
        {
            using ValueList<int> list = new ValueList<int>(stackalloc int[Size]);
            for (int i = 0; i < Size; i++)
            {
                list.Add(i);
            }
        }

        [Benchmark]
        public void ValueListWithInitialCapacity()
        {
            using ValueList<int> list = new ValueList<int>(Size);
            for (int i = 0; i < Size; i++)
            {
                list.Add(i);
            }
        }
    }
}
