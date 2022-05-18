using System.Buffers;
using System.Collections;

namespace TraceRtLive.Helpers
{
    /// <summary>
    /// Creates a thread-safe, First-In, First-Out circular buffer of a fixed size.
    /// Non-allocating on add, but does allocate when <see cref="IEnumerator{T}">iterating</see>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        /// <summary>
        /// Create a new buffer of the specified <paramref name="size"/>.
        /// </summary>
        /// <param name="size"></param>
        public CircularBuffer(int size)
        {
            _pool = ArrayPool<T>.Create(size, 3);
            Size = size;
            _current = size-1; // initialize to end
            _buffer = new T[Size];
        }

        /// <summary>
        /// Actual buffer contents
        /// </summary>
        private T[] _buffer;

        /// <summary>
        /// Buffers used for <see cref="GetEnumerator"/>
        /// Locked by <see cref="_lock"/>.
        /// </summary>
        private ArrayPool<T> _pool;

        /// <summary>
        /// Number of items allocated in <see cref="_buffer"/>
        /// Locked by <see cref="_lock"/>.
        /// </summary>
        private int _count;

        /// <summary>
        /// Current index in <see cref="_buffer"/>.
        /// Locked by <see cref="_lock"/>.
        /// </summary>
        private int _current;

        /// <summary>
        /// Lock for modifying <see cref="_buffer"/>, <see cref="_count"/> and <see cref="_current"/>.
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// Maximum size of the buffer. See also <seealso cref="Count"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Current number of items in the buffer,
        /// will never be more than <see cref="Size"/>.
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Add an item to the buffer, replacing the oldest item if <see cref="Count"/>
        /// exceeds <see cref="Size"/>.
        /// </summary>
        /// <param name="value">Value to add</param>
        public void Add(T value)
        {
            lock (_lock)
            {
                if (++_current >= Size) _current = 0;
                if (_count < Size) _count++;

                _buffer[_current] = value;
            }
        }

        /// <summary>
        /// Iterate through the current items.
        /// This allocates a new array of the same <see cref="Size"/>
        /// and copies the current items to it, so that iterating is not
        /// affected by concurrent calls to <see cref="Add(T)"/>.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            int startIdx;
            int count;
            var temp = _pool.Rent(Size);
            lock (_lock)
            {
                startIdx = _current + 1;
                count = _count;
                Array.Copy(_buffer, 0, temp, 0, _count);
            }

            if (_count < Size)
            {
                // partial
                for (var i = 0; i < _count; i++) yield return temp[i];
            }
            else
            {
                for (var i = startIdx; i < Size; i++) yield return temp[i];
                for (var i = 0; i < startIdx; i++) yield return temp[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
