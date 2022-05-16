namespace TraceRtLive.Helpers
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// Get the first item from an <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type being iterated</typeparam>
        /// <param name="source">Collection to find the first item of</param>
        public static async Task<T?> FirstOrDefaultAsync<T>(this IAsyncEnumerable<T> source)
        {
            await foreach (var item in source)
            {
                return item;
            }
            return default;
        }

        /// <summary>
        /// Get the first item from an <see cref="IAsyncEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type being iterated</typeparam>
        /// <param name="source">Collection to find the first item of</param>
        /// <exception cref="InvalidOperationException">The source sequence is empty</exception>
        public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> source)
        {
            await foreach (var item in source)
            {
                return item;
            }
            throw new InvalidOperationException("The source sequence is empty");
        }
    }
}
