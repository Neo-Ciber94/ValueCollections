using System;
using System.Buffers;
using BenchmarkDotNet.Running;

namespace ExtraUtils.ValueCollections.Benchmarks
{
    class Program
    {
        static void Main()
        {
            BenchmarkRunner.Run<ListVsValueListBenchmarks>();
        }
    }
}
