using System.Diagnostics;

namespace ExtraUtils.ValueCollections
{
    internal class ValueArrayDebugView<T>
    {
        private readonly T[] _array;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _array;

        public ValueArrayDebugView(ValueArray<T> array)
        {
            _array = array.ToArray();
        }
    }
}
